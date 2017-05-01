using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GoRecycler
{
    [System.Serializable]
    public class GoRecycleBin : System.Object
    {
        #region Fields

        [SerializeField]
        private string label;

        public string Label { get { return label; } }

        [SerializeField]
        private int MaxItems;

        [SerializeField]
        private GameObject GoPrefab;

        [SerializeField]
        private bool Preload;

        [SerializeField]
        private int Preloadcount;

        [SerializeField]
        private float PreloadIterationTime = 0.01f;
        
        private Stack<GameObject> PooledObjects = new Stack<GameObject>();
        
        private MonoBehaviour behavior;

        #endregion

        #region Public API 

        private int mCount = 0;

        /// <summary>
        /// Returns the object pool size
        /// </summary>
        public int ObjectCount
        {
            get
            {
                return mCount;
            }
        }

        /// <summary>
        /// If you're using GoRecycleBin in a custom script you MUST inicialize it on Awake() 
        /// to make the Recycle Bin Work correctly
        /// </summary>
        /// <param name="behavior">your custom MonoBehavior</param>
        public void InitPool(MonoBehaviour behavior)
        {
           
            if (CanInit)
            {
                this.behavior = behavior;
                behavior.StartCoroutine(IGenerateGO());
            }
        }
        
        /// <summary>
        /// Recycles an object from the object pool
        /// </summary>
        /// <param name="InstanceId">Instance id of the object</param>
        public void Recycle(GameObject go )
        {
            if (!PooledObjects.Contains(go) && PooledObjects.Count < MaxItems )
            {
                IPooled ip = go.GetComponent<IPooled>();
                if (ip != null)
                {
                    ip.OnRecycle(this);
                }
                PooledObjects.Push(go);
                go.SetActive(false);
                go.transform.position = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Gets an GameObject from the Object pool
        /// </summary>
        /// <param name="Position">Target Position</param>
        /// <param name="Rotation">Target Rotation</param>
        /// <returns>Returns a Gameobject if it is aviable, otherwise returns null</returns>
        public GameObject GetGameObject(Vector3 Position, Quaternion Rotation)
        {
            if (MaxItems > 0 )
            {            
                if ( PooledObjects.Count <= 0 )  RegisterGO();
                GameObject curr = GetAndEnable(Position , Rotation);
                return curr;
            }
            return null; 
        }

        /// <summary>
        /// Emptys the entrie recycle bin
        /// </summary>
        /// <param name="destroyObjects"></param>
        public void EmptyRecycleBin(bool destroyObjects = false)
        {
            while (PooledObjects.Count > 0 )
            {
                GameObject obj = PooledObjects.Pop();
                if (destroyObjects)
                {
                    Object.Destroy(obj);
                    mCount--;
                }
            }
        }

        #endregion

        #region Private API 
        
        private GameObject GetAndEnable(Vector3 pos , Quaternion rot )
        {
            if (PooledObjects.Count > 0 )
            {
                GameObject g = PooledObjects.Pop();
                g.transform.position = pos;
                g.transform.rotation = rot;
                g.SetActive(true);

                IPooled ip = g.GetComponent<IPooled>();
                if (ip != null)
                {
                    ip.OnSpawn(this);
                }
                return g;
            }
            return null; 
        }
        
        private bool CanInit
        {
            get
            {
                return Preload & Preloadcount > 0;
            }
        }

        private IEnumerator IGenerateGO()
        {
            for (int i = 0; i < Preloadcount; i++)
            {
                RegisterGO();
                yield return new WaitForSeconds(PreloadIterationTime);
            }
            behavior.StopCoroutine(IGenerateGO());
        }

        private GameObject RegisterGO()
        {
            if (ObjectCount < MaxItems )
            {
                if (GoPrefab != null)
                {
                    GameObject Clone = Object.Instantiate(GoPrefab, Vector3.zero, Quaternion.identity);
                    Clone.transform.parent = behavior != null ? behavior.gameObject.transform : null;

                    Recycle(Clone);

                    mCount++;

                    return Clone;
                }
            }
            return null;
        }
        #endregion
    }
}