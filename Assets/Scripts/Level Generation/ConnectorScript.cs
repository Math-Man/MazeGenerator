using UnityEngine;

namespace Level_Generation
{
    public class ConnectorScript : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = Vector2.one * 8;
        public bool isConnected;
        public TileScript connectedTile;
        public ConnectorScript connectedConnector;
        private bool m_IsPlaying;
        public Vector2 GetSize() => _size;
        
        private void Start()
        {
            m_IsPlaying = true;
        }

        private void OnDrawGizmos()
        {
            void DrawConnectorGizmo()
            {
                var halfSize = (_size * 0.5f);
                var transform1 = transform;
                var up = transform1.up;
                var position = transform1.position;
                var offset = position + up * halfSize.y;
                var top = up * _size.y;
                var side = transform1.right * halfSize.x;
                var topRight = position + top + side;
                var topLeft = position + top - side;
                var bottomRight = position + side;
                var bottomLeft = position - side;

                if(!m_IsPlaying) 
                    Gizmos.color = Color.cyan;
                else
                    Gizmos.color = isConnected ? Color.green : Color.red;
                
                Gizmos.DrawLine(offset, offset + transform1.forward);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, offset);
                Gizmos.DrawLine(topLeft, offset);
                Gizmos.DrawLine(bottomRight, offset);
                Gizmos.DrawLine(bottomLeft, offset);
            }

            DrawConnectorGizmo();
        }
    }
}