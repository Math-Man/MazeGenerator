using System;
using System.Collections.Generic;
using System.Linq;
using Level_Generation.Scriptable;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Level_Generation
{
    public class LevelBuilder : MonoBehaviour
    {
        
        [Header("Generation Settings")]
        [SerializeField] private TileType _startTileType = TileType.Cap;
        [SerializeField] private TileData[] _allTileData;
        [SerializeField] private ConnectorPositionWrapper[] _connectorBlockers;
        [SerializeField] private ConnectorPositionWrapper[] _doorwayPrefabs;
        [SerializeField] private ConnectorPositionWrapper[] _doorPrefabs;
        [SerializeField] [Range(0f, 1f)] private float _doorwayChance = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _doorwayHavingDoorChance = 0.5f;
        [SerializeField] private string _tileLayerName = "Tile";
        [SerializeField] private string _doorLayerName = "Door";
        
        [Header("Pathing restrictions")] 
        [SerializeField] private int _mainPathTileCount = 10;
        [SerializeField] private int _maxNumberOfAttemptsPerTile = 100;
        [SerializeField] private int _maxFailureCount = 100;
        [SerializeField] private TileType _mainBranchEndingTileType = TileType.Cap;
        
        [Header("Branching")]
        [SerializeField] [Range(0f, 1f)] private float _branchingChance = 0.5f;
        [SerializeField] private int _virtualBranchingCap = 20;
        [SerializeField] private int _branchesMinTileCount = 1;
        [SerializeField] private int _branchesMaxTileCount = 10;

        [Header("Optimization")] 
        [SerializeField] private bool _cacheMeshComponents = true;

        private List<TileBranch> _Branches;
        private List<MeshFilter> _tileMeshFilters;
        private List<MeshRenderer> _tileMeshRenderers;
        
        private void Awake()
        {
            if (_allTileData == null || _allTileData.Length == 0)
            {
                Debug.LogError("All tile data is empty");
            }
            _Branches = new List<TileBranch>();

            _tileMeshFilters = new List<MeshFilter>();
            _tileMeshRenderers = new List<MeshRenderer>();
        }
        

        private void Start()
        {
            BuildLevel();
        }


        private void BuildLevel()
        {
            //Create the initial tile
            TileScript rootTile = SpawnRandomTileAndConnect(default(TileBranch), null, TileType.Cap, transform, null, true);
            TileScript tileTo = rootTile;

            if (_cacheMeshComponents)
            {
                _tileMeshRenderers.Add(gameObject.GetComponent<MeshRenderer>());
                _tileMeshFilters.Add(gameObject.GetComponent<MeshFilter>());
                
                _tileMeshRenderers.Add(tileTo.GetComponent<MeshRenderer>());
                _tileMeshFilters.Add(tileTo.GetComponent<MeshFilter>());
            }

            
            var mainBranch = BuildBranch(_mainPathTileCount, tileTo, "Main", _mainBranchEndingTileType);
            _Branches.Add(mainBranch);
            HandleBranching(rootTile);
            HandleAllBranchConnectors();

            if (_cacheMeshComponents)
            {
                _tileMeshFilters.RemoveAll(tmf => tmf == null);
                _tileMeshRenderers.RemoveAll(tmf => tmf == null);
            }

        }


        private TileBranch BuildBranch(int branchLength, TileScript tileTo, string branchIdentifier, TileType endingRoomType = TileType.Default, bool canDeleteRoot = true)
        {
            TileBranch branch = new TileBranch(tileTo);
            
            var containerObject = new GameObject($"Branch {branchIdentifier}");
            Transform containerTransform = containerObject.transform;
            containerTransform.SetParent(transform);
            
            List<TileScript> visitedTiles = new List<TileScript>();
            
            var originalRoot = tileTo;
            int failureCount = 0;

            TileScript tileFrom = null;
            for (int i = 0 ; i < branchLength; i++)
            {
                tileFrom = tileTo != null ? tileTo : tileFrom;

                if (endingRoomType != TileType.Default && (i == branchLength - 1))
                {
                    tileTo = SpawnRandomTileAndConnect(branch, tileFrom, endingRoomType, containerTransform, visitedTiles, false);
                }
                else
                {
                    tileTo = SpawnRandomTileAndConnect(branch, tileFrom, tileFrom.GetTileData().validConnectionTypes, containerTransform, visitedTiles, false);
                }


                Debug.Log("Placement "+ i);
                
                if (tileTo == null)
                {
                    if (originalRoot == tileTo || originalRoot == tileFrom)
                    {
                        Debug.Log("Trying to delete the original parent!", this);
                        if (!canDeleteRoot)
                        {
                            Debug.Log("Can't delete the original parent! Stopping.", tileTo);
                            break;
                        }
                    }

                    Debug.Log("Total failure: " + (failureCount + 1) + " " + i);
                    
                    if (_maxFailureCount < failureCount)
                        break;

                    i--;
                    failureCount++;
                    tileTo = tileFrom;
                    tileFrom = tileFrom?.parentTile;

                    Debug.Log("Deleting last tile " + i);
                    branch.BranchTiles.Remove(tileTo);
                    DisconnectAndDeleteConnectedTile(tileTo, tileTo.parentedConnector);
                }
                else
                {
                    if (_cacheMeshComponents)
                    {
                        _tileMeshFilters.Add(tileTo.GetComponent<MeshFilter>());
                        _tileMeshRenderers.Add(tileTo.GetComponent<MeshRenderer>());
                    }
                    failureCount = 0;
                }
            }
            return branch;
        }


        private void HandleBranching(TileScript rootTile, ConnectorScript source = null, int currentBranchCount = 0, int branchDepth = 0)
        {
            if (currentBranchCount >= _virtualBranchingCap)
                return;
            
            // Look through the connected tiles
            for (int connectorIndex = 0; connectorIndex < rootTile.GetConnectors().Length; connectorIndex++) 
            {
                // If a connector is connected:
                
                // (ignore the source connector) 
                var connector = rootTile.GetConnectors()[connectorIndex];
                if(connector.connectedConnector == source) continue;
                
                //jump to that tile
                if (connector.isConnected)
                {
                    var nextTile = connector.connectedTile;
                    // Call Branching on that Tile with the same depth
                    
                    HandleBranching(nextTile, connector, currentBranchCount, branchDepth);
                }
                // If a connector is not connected:
                else
                {
                    // If the random branch creation chance passes :
                    if (_branchingChance < Random.value) continue;

                    if (rootTile == null)
                    {
                        Debug.LogError("EVERYTHING IS TERRIBLE");
                        return;
                    }

                    var branch = BuildBranch(
                        Random.Range(_branchesMinTileCount, _branchesMaxTileCount),
                        rootTile,
                        $"From Tile {rootTile}",
                        TileType.Default,
                        false);
                    
                    currentBranchCount++;
                    _Branches.Add(branch);
                }
            }
        }


        private void ConnectTiles(TileScript tileFrom, TileScript tileTo, Transform currentContainer,
            ConnectorScript connectorFrom, ConnectorScript connectorTo)
        {
            ParentedTilePlacement(tileTo, currentContainer, connectorTo, connectorFrom);
            
            //Mark as connected and handle wiring
            connectorTo.isConnected = true;
            connectorTo.connectedConnector = connectorFrom;
            connectorTo.connectedTile = tileFrom;
                
            connectorFrom.isConnected = true;
            connectorFrom.connectedConnector = connectorTo;
            connectorFrom.connectedTile = tileTo;
            
            tileTo.parentTile = tileFrom;
            tileTo.parentedConnector = connectorFrom;
        }
        
        private void DisconnectAndDeleteConnectedTile(TileScript tileTo, ConnectorScript connectorFrom)
        {
            if (connectorFrom != null)
            {
                connectorFrom.isConnected = false;
                connectorFrom.connectedConnector = null;
                connectorFrom.connectedTile = null;
            }

            if (tileTo != null)
            {
                tileTo.parentTile = null;
                tileTo.parentedConnector = null;
                DestroyImmediate(tileTo.gameObject);
            }
        }


        private void SpawnDoorForConnector(ConnectorScript connectorScript)
        {
            bool hasDoorwayPrefabs = !(_doorwayPrefabs == null || _doorwayPrefabs.Length == 0);
            bool hasDoorPrefabs = !(_doorPrefabs == null || _doorPrefabs.Length == 0);
            
            if (_doorwayChance <= 0f || (!hasDoorwayPrefabs && !hasDoorPrefabs)) return;

            if (Random.Range(0f, 1f) < _doorwayChance)
            {
                if (hasDoorwayPrefabs)
                {
                    if (Random.Range(0f, 1f) < _doorwayHavingDoorChance)
                    {
                        if (!hasDoorPrefabs) return;
                        // Spawn door
                        CreateDoor(connectorScript);
                    }
                    else
                    {
                        // Spawn doorway
                        CreateDoorWay(connectorScript);
                    }
                }
                else
                {
                    // Spawn door in place of doorway
                    if (!hasDoorPrefabs) return;
                        CreateDoor(connectorScript);
                }
            }
        }

        private bool DoorCollisionsCheck(ConnectorScript connectorScript)
        {
            Transform connectorTransform = connectorScript.transform;
            Vector3 doorHalfExtens = new Vector3(connectorScript.GetSize().x, 1f, connectorScript.GetSize().x);
            Vector3 doorPosition = connectorTransform.position;
            Vector3 offset = Vector3.up * 0.5f;
            
            Collider[] hits = Physics.OverlapBox(doorPosition + offset, doorHalfExtens, Quaternion.identity,
                LayerMask.GetMask(_doorLayerName));

            return hits.Length == 0;
        }

        private GameObject CreateDoorWay(ConnectorScript connectorScript)
        {
            Transform transform1 = connectorScript.transform;
            ConnectorPositionWrapper randomDoorway = _doorwayPrefabs[Random.Range(0, _doorwayPrefabs.Length)];
            
            var doorway = Instantiate(randomDoorway.connector,
                transform1.position,
                transform1.rotation,
                transform1);

            doorway.transform.localPosition +=  randomDoorway.localPositionOffset;
            doorway.transform.localRotation *= randomDoorway.localRotationOffset;
            
            return doorway;
        }

        private GameObject CreateDoor(ConnectorScript connectorScript)
        {
            Transform transform1 = connectorScript.transform;
            ConnectorPositionWrapper randomDoor = _doorPrefabs[Random.Range(0, _doorPrefabs.Length)];
            
            var doorway = Instantiate(randomDoor.connector,
                transform1.position,
                transform1.rotation,
                transform1);
            
            doorway.transform.localPosition +=  randomDoor.localPositionOffset;
            doorway.transform.localRotation *= randomDoor.localRotationOffset;
            
            return doorway;
        }
        
        private void SpawnBlockerForConnector(ConnectorScript connector)
        {
            var randomBlocker = _connectorBlockers[Random.Range(0, _connectorBlockers.Length)];
            var transform1 = connector.transform;
            var blockerInstance = GameObject.Instantiate(randomBlocker.connector,
                transform1.position,
                transform1.rotation,
                transform1);
            
            blockerInstance.transform.localPosition +=  randomBlocker.localPositionOffset;
            blockerInstance.transform.localRotation *= randomBlocker.localRotationOffset;
            
            blockerInstance.name = $"{blockerInstance.name}";
        }


        
        /**
         * Blocks empty passages and spawns doors for connected passages
         */
        private void HandleAllBranchConnectors()
        {
            if (_connectorBlockers == null || _connectorBlockers.Length == 0)
            {
                Debug.LogWarning("No blocker prefabs are designated", this);
                return;
            }

            int count = 0;
            foreach (var branch in _Branches)
            {
                foreach (var tile in branch.BranchTiles)
                {
                    foreach (var connector in tile.GetConnectors())
                    {
                        
                        //Has no connection, spawn a blocker
                        if (!connector.isConnected)
                        {
                            count++;
                            Debug.Log($"Non-connected Connector @{branch} -> @{tile} -> @{connector}",
                                connector.gameObject);

                            SpawnBlockerForConnector(connector);
                        }
                        //Has connection try to spawn a door
                        else
                        {
                            if(DoorCollisionsCheck(connector))
                                SpawnDoorForConnector(connector);
                        }
                    }
                }
            }

            Debug.Log($"Non-connected count {count}");
        }

        private void ParentedTilePlacement(TileScript tileTo, Transform currentContainer, ConnectorScript pickedConnectorTo,
            ConnectorScript pickedConnectorFrom)
        {
            //Apply primary rotation by random
            float upRotation = Random.Range(0, 4) * 90f;
            tileTo.transform.Rotate(0f, upRotation, 0f);
            
            //Set parented placement
            Transform transform1;
            (transform1 = pickedConnectorTo.transform).SetParent(pickedConnectorFrom.transform);
            tileTo.transform.SetParent(transform1);

            var transform2 = pickedConnectorTo.transform;
            transform2.localPosition = Vector3.zero;
            transform2.localRotation = Quaternion.identity;
            pickedConnectorTo.transform.Rotate(0f, 180f, 0f);

            Transform transform3;
            (transform3 = tileTo.transform).SetParent(currentContainer);
            pickedConnectorTo.transform.SetParent(transform3);
            pickedConnectorTo.transform.SetParent(tileTo.GetConnectorContainer());
        }

        private TileScript SpawnRandomTileAndConnect(TileBranch branch, TileScript parentTile, TileType type, Transform tileContainer,
            List<TileScript> excludedTiles = null, bool initialTile = false)
        {
            return SpawnRandomTileAndConnect(branch, parentTile, new []{ type }, tileContainer, excludedTiles, initialTile);
        }

        private TileScript SpawnRandomTileAndConnect(TileBranch branch, TileScript parentTile, TileType[] types, Transform tileContainer,
            List<TileScript> excludedTiles = null, bool initialTile = false)
        {
            for (int attempt = 0; attempt < _maxNumberOfAttemptsPerTile; attempt++)
            {
                var randomTile = RandomSelectTileData(
                    initialTile ? GetTilesOfType(TileType.Cap, _allTileData) : GetTilesOfTypes(types, _allTileData));


                randomTile.name = initialTile ?  $"Starting Tile: {randomTile.referenceName}" : $"Tile: {randomTile.referenceName}";
                
                var instantiatedTile = GameObject.Instantiate(randomTile.tilePrefab, Vector3.zero,
                    Quaternion.identity, tileContainer);
                instantiatedTile.Init();
                
                if (!initialTile)
                {            
                    var pickedConnectorFrom = parentTile.GetRandomUnconnectedConnector();
                    var pickedConnectorTo = instantiatedTile.GetRandomUnconnectedConnector();

                    if (pickedConnectorFrom != null && pickedConnectorTo != null)
                    {
                        ConnectTiles(parentTile, instantiatedTile, tileContainer, pickedConnectorFrom, pickedConnectorTo);
                        branch.BranchTiles.Add(instantiatedTile);

                        instantiatedTile.CreateBoxCollider();
                        if(instantiatedTile.HasCollisionWithAnotherTile() == 0)
                            return instantiatedTile;
                    }
                    
                    //Failed to place
                    branch.BranchTiles.Remove(instantiatedTile);
                    DisconnectAndDeleteConnectedTile(instantiatedTile, pickedConnectorFrom);
                    Debug.Log("failed to place");
                }
                else
                    return instantiatedTile;
            }
            
            //Couldn't place a tile adjacent to the given tile
            return null;
        }

        private TileData WeightedRandomSelectTileData(TileData[] selectionSet)
        {
            TileData selected = null;
            float sum = 0;
            foreach(TileData tileData in selectionSet)
            {
                sum += tileData.selectionWeight;
                if (tileData.selectionWeight <= Random.Range(0f, sum))
                {
                    selected = tileData;
                }
            }
            return selected;
        }
        
        private TileData RandomSelectTileData(TileData[] selectionSet)
        {
            return selectionSet[Random.Range(0, selectionSet.Length - 1)];
        }

        private TileData[] GetTilesOfType(TileType type, TileData[] selectionSet)
        {
            return selectionSet.Where(tile => tile.tileType == type).ToArray();
        }
        
        private TileData[] GetTilesOfTypes(TileType[] types, TileData[] selectionSet)
        {
            return selectionSet.Where(tile =>
            {
                foreach (var validType in types)
                    if(tile.tileType == validType)
                        return true;
                return false;
            }).ToArray();
        }


        [Serializable]
        private struct TileBranch
        {
            public List<TileScript> BranchTiles;
            public TileScript sourceTile;
            public TileBranch(TileScript sourceTile)
            {
                this.BranchTiles = new List<TileScript>();
                this.sourceTile = sourceTile;
            }
        }
        
        [Serializable]
        private struct ConnectorPositionWrapper
        {
            public GameObject connector;
            public Vector3 localPositionOffset;
            public Quaternion localRotationOffset;
            
            public ConnectorPositionWrapper(GameObject connector, Vector3 localPositionOffset, Quaternion localRotationOffset)
            {
                this.connector = connector;
                this.localPositionOffset = localPositionOffset;
                this.localRotationOffset = localRotationOffset;
            }
        }

    }
}