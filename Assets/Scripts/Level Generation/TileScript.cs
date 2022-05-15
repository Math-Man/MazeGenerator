using System.Collections.Generic;
using System.Linq;
using Level_Generation.Scriptable;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Level_Generation
{
    public class TileScript : MonoBehaviour
    {
        [SerializeField] private TileData tileData;
        [SerializeField] private bool findRelevantObjectsAutomatically = true;
        [Header("Object references (Auto-Completed fields)")]
        [SerializeField] private ConnectorScript[] connectors;
        [SerializeField] private Light[] lights;
        [SerializeField] private Transform connectorContainer;
        [SerializeField] private string tileLayerName = "Tile";
        
        
        public TileScript parentTile { get; set; }
        public ConnectorScript parentedConnector { get; set; }

       
        public TileData GetTileData() => tileData;
        public ConnectorScript[] GetConnectors() => connectors;
        public Light[] GetLights() => lights;
        public Transform GetConnectorContainer() => connectorContainer;

        private BoxCollider m_BoxCollider;
        

        public void Init()
        {
            if(findRelevantObjectsAutomatically)
                FindRelevantObjects();
        }

        private void FindRelevantObjects()
        {
            connectors = transform.GetComponentsInChildren<ConnectorScript>();
            lights = transform.GetComponentsInChildren<Light>();

            if (connectors != null && connectors.Length > 0)
            {
                connectorContainer = connectors[0].transform.parent;
            }
        }

        
        public ConnectorScript GetRandomUnconnectedConnector()
        {
            var filteredConnectors = connectors.Where(connector => connector.isConnected == false).ToArray();
            if(filteredConnectors.Length > 0)
                return filteredConnectors[Random.Range(0, filteredConnectors.Length)];

            return null;
        }
        
        public ConnectorScript GetRandomUnconnectedConnectorWithExcludedTiles(List<TileScript> excludedTiles)
        {
            if (excludedTiles == null || excludedTiles.Count == 0)
                return GetRandomUnconnectedConnector();
            
            var filteredConnectors = 
                connectors.Where(connector => connector.isConnected == false && 
                                              excludedTiles.Where(tile => 
                                                      tile.connectors.Any(connectorScript => connectorScript.connectedConnector.Equals(connector)))
                                                  .ToList().Count == 0)
                    .ToArray();
            return filteredConnectors[Random.Range(0, filteredConnectors.Length)];
        }


        public int HasCollisionWithAnotherTile()
        {
            var center = m_BoxCollider.center;
            var transform1 = transform;
            var offset = transform1.right * center.x + transform1.up * center.y + transform1.forward * center.z;
            var halfExtents = m_BoxCollider.bounds.extents;

            var colliderHits = Physics.OverlapBox(transform1.position + offset, halfExtents, Quaternion.identity,
                LayerMask.GetMask(tileLayerName));

            foreach (var colliderHit in colliderHits)
            {
                if (colliderHit.transform != transform1 && colliderHit.transform != parentTile.transform)
                {
                    return 1;
                }
            }
            return 0;
        }

        public void CreateBoxCollider()
        {
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            m_BoxCollider = boxCollider;
        }

        public void SetBoxColliderActive(bool state)
        {
            if (m_BoxCollider != null)
                m_BoxCollider.enabled = state;
        }

    }
}