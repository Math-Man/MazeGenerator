using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Level_Generation.Scriptable
{
    [CreateAssetMenu(fileName = "Tile Data", menuName = "Game Data/Tile Data", order = 0)]
    public class TileData : ScriptableObject
    {
        [Header("Generation Settings")] 
        public string referenceName = "";
        public float selectionWeight = 1f;
        public TileType tileType = default(TileType);
        public TileType[] validConnectionTypes = (TileType[]) Enum.GetValues(typeof(TileType));


        [Header("Mesh and Bounds")] 
        public TileScript tilePrefab;
        public bool autoFindMesh;
        public Mesh mesh;
    
        private void OnValidate()
        {
            if(string.IsNullOrEmpty(referenceName))
                referenceName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));

            MeshFilter meshFilter;
            if (autoFindMesh && tilePrefab != null &&
                tilePrefab.gameObject.TryGetComponent<MeshFilter>(out meshFilter))
            {
                mesh = meshFilter.sharedMesh;
            }
        }
    
    }

    public enum TileType
    {
        Default, Cap, Hall, Room
    }
}