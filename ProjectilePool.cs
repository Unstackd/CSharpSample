using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A pool specifically optimized for Projectile GameObjects.
/// </summary>
public class ProjectilePool : MonoBehaviour
{

    #region Singleton
    /// <summary>
    /// Singleton instance of the ProjectilePool. Singleton is particularly important here because it will let us avoid costly GetComponent calls elsewhere.
    /// </summary>
    public static ProjectilePool Instance { get; private set; }

    /// <summary>
    /// Called by the engine when this component is created.
    /// </summary>
    void Awake()
    {
        DebugUtil.Assert(Instance == null);
        Instance = this;
    }

    #endregion



    /// <summary>
    /// The path of the projectile prefab we use as a template for all the projectiles we will pool.  Use a constant instead of configuring via inspector to avoid accidental untracked changes.
    /// </summary>
    private const string PROJECTILE_PREFAB_PATH = "WeaponParts/PooledProjectile";

    /// <summary>
    /// The default number of projectiles we should have in the pool.
    /// </summary>
    [SerializeField]
    private int _defaultBufferAmount = 80;

    /// <summary>
    /// The container object that we will keep unused pooled objects so we dont clog up the editor with objects.
    /// </summary>
    private GameObject _containerObject;

    /// <summary>
    /// The cached transform of our container object.
    /// </summary>
    private Transform _containerObjectTransform;

    /// <summary>
    /// The datastructure that will hold our projectiles.
    /// </summary>
    private Queue<GameObject> _pooledProjectiles = new Queue<GameObject>();

    
    /// <summary>
    /// Called by the engine soon after the pool is first created.
    /// </summary>
    void Start()
    {
        //Create and initialize the object heirarchy that will hold all our pooled projectiles.
        _containerObject = new GameObject("ProjectilePool");
        _containerObjectTransform = _containerObject.transform;
        DontDestroyOnLoad(_containerObject);

        //Initialize the pool with the default amount
        while (_pooledProjectiles.Count < _defaultBufferAmount)
        {//While we have less than our desired number of projectiles in the pool...
            PoolObject(((GameObject)Instantiate(Resources.Load(PROJECTILE_PREFAB_PATH))));//Instantiate another one
        }

    }

    public static GameObject GetProjectileStatic()
    {
        GameObject projectileGameObject = Instance.GetProjectile();
        Projectile projectileComponent = Util.QuickGetComponent<Projectile>(projectileGameObject);

        //We used to call projectileGameObject.SendMessage("OnGetFromPool") here but unity uses reflection on every method of every component on the target gameobject which is extremely slow. CallOnGetFromPoolOnAllComponents is a method that Projectile.cs has that accomplishes the same thing with dispatches, and much fewer of them.
        projectileComponent.CallOnGetFromPoolOnAllComponents();
        
        return projectileGameObject;
    }

    /// <summary>
    /// Gets a projectile from the pool.  Much more efficient than instantiating a new projectile.
    /// </summary>
    /// <returns></returns>
    private GameObject GetProjectile()
    {
        if (_pooledProjectiles.Count > 0)
        {
            GameObject pooledProjectile = _pooledProjectiles.Dequeue();
            pooledProjectile.transform.parent = null;

            pooledProjectile.SetActive(true);

            return pooledProjectile;
        }
        else
        {//If we ran out of projectiles in the pool (this is rare)...
            //We will have to suffer the cost of instantiating a new one.  Fortunately, this new one will can go back into the pool and make it less likely to happen in the future.
            GameObject newlyInstantiatedObject = ((GameObject)Instantiate(Resources.Load(PROJECTILE_PREFAB_PATH)));//Instantiate a new projectile.  We don't hold onto the Resources.Load reference because this does not happen often and we prefer Unity's caching of prefabs.

            return newlyInstantiatedObject;
        }
    }

    /// <summary>
    /// Pools Projectile projectileComponentToPool which should be sitting on GameObject gameObjectToPool.  We pass both here to avoid a GetComponent which will cause performance issues with rapid fire guns.  It is recommended to use this static method instead of calling PoolObject(GameObject obj) directly in cases where one would need to do an additional GetComponent<ProjectilePool>() to get a handle on this also for performance reasons.
    /// </summary>
    /// <param name="gameObjectToPool">The GameObject of the projectile we wish to pool.</param>
    /// <param name="projectileComponentToPool">The Projectile component of the projectile we wish to pool.</param>
    public static void PoolProjectileStatic(GameObject gameObjectToPool, Projectile projectileComponentToPool)
    {
        //Make sure the component and gameObject belong to each other when testing.  This call is stripped in a real build so we don't suffer the .gameobject performance penalty in a real build.
        DebugUtil.Assert(projectileComponentToPool.gameObject == gameObjectToPool);

        //We used to call g.SendMessage("OnReturnToPool") here but unity uses reflection on every method of every component on the target gameobject which is extremely slow. CallOnReturnToPoolOnAllComponents() is a method that Projectile.cs has that accomplishes the same thing with dispatches, and much fewer of them.
        projectileComponentToPool.CallOnReturnToPoolOnAllComponents();
        Instance.PoolObject(gameObjectToPool);//Return the object to the pool
    }

    /// <summary>
    /// Pools the passed projectile.
    /// </summary>
    /// <param name='gameObjectToPool'>
    /// Gameobject of the projectile to be pooled.
    /// </param>
    public void PoolObject(GameObject gameObjectToPool)
    {
        gameObjectToPool.SetActive(false);
        
        //When debug mode is enabled, these asserts check for a specific kind of mistake which can occur when Monobehaviour.Destroy is called and we later try to pool the C# object before the engine has cleaned it up.  In a real build they are stripped for performance.
        DebugUtil.Assert(_containerObject != null);
        DebugUtil.Assert(_containerObjectTransform != null);
        DebugUtil.Assert(gameObjectToPool != null);
        DebugUtil.Assert(gameObjectToPool.transform != null);

        gameObjectToPool.transform.parent = _containerObjectTransform;
        _pooledProjectiles.Enqueue(gameObjectToPool);
    }

    /// <summary>
    /// Returns the number of projectiles currently stored in the projectile pool.
    /// </summary>
    /// <returns></returns>
    public int CountObjects()
    {
        //We will use an indirect method like this to make it easier to change the datastructure that holds projectiles later.  Sometimes optimizations in the engine change which datastructure is best used to back a pool.
        return _pooledProjectiles.Count;
    }
}


