/*
 * Singleton.cs
 * 
 * - Unity Implementation of Singleton template
 * 
 */

using UnityEngine;

using System;
using UnityEngine.SceneManagement;
/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
///
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
///


public enum SingletonLifeCycleType
{
    LiveForever,
    LiveSceneOnly

}

public class Singleton  : MonoBehaviour
{
    public virtual void Destroy()
    {
      
    }

}
[DefaultExecutionOrder(-1)]
public class Singleton<T> : Singleton where T : Singleton<T>
{
    private static T _instance;
    public static bool Exists => _instance != null;
    private static T instance
    {
        set
        {
            _instance = value;
            if (_instance != null)
            {
                var lifeCycleType = _instance.GetSingletonLifeCycleType();
                if (lifeCycleType == SingletonLifeCycleType.LiveForever)
                {
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isPlaying)
#endif
                    DontDestroyOnLoad(_instance.transform.root.gameObject);

                }
            }

        }
        get
        {
            return _instance;
        }
    }

    private static object _lock = new object();

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            SingletonInitialize();
        }
        else if (_instance != this)
        {
#if UNITY_EDITOR
            if(!Application.isPlaying && UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(_instance))
            {
                DestroyImmediate(_instance.gameObject);
                _instance = this as T;
                SingletonInitialize();
                return;
            }
#endif

            Debug.LogError($"Duplicate singleton : {gameObject.name}");
            Destroy(gameObject);
        }
    }

    //static bool applicationQuiting = false;
    //void OnApplicationQuit()
    //{
    //    applicationQuiting = true;
    //}

    public static T Instance
    {
        get
        {

//             if ((AppController.ApplicationQuiting)
// #if UNITY_EDITOR
//                  && UnityEditor.EditorApplication.isPlaying
// #endif
//                   || SceneLoadingManager.sceneUnloading
//                )
//             {
//                 //				Debug.Log ("[Singleton] Instance '" + typeof(T) +
//                 //					"' already destroyed on application quit." +
//                 //					" Won't create again.");
//                 return instance;
//             }


            lock (_lock)
            {
                if (instance == null)
                {
                    instance = (T)FindObjectOfType(typeof(T));

                    if (instance == null)
                    {
                        GameObject singleton = new GameObject();
                        singleton.SetActive(false);
                        instance = singleton.AddComponent<T>();
                        instance.gameObject.name = "~" + typeof(T).ToString();
                        singleton.SetActive(true);
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(singleton, scene);
                        }
#endif
                    }
                    else
                    {

                        //Debug.Log("Singleton(" + typeof(T).ToString() + ") Using instance already created: " +
                                   //instance.gameObject.name);
                    }


                    instance.SingletonInitialize();
                }

                return instance;
            }
        }
    }

    protected virtual void SingletonInitialize()
    {

    }

    protected virtual SingletonLifeCycleType GetSingletonLifeCycleType()
    {
        return SingletonLifeCycleType.LiveSceneOnly;
    }

    public override void Destroy()
    {
        if(this != null && gameObject != null)
        {
            GameObject.DestroyImmediate(gameObject);
        }
        
        _instance = null;
    }
}

