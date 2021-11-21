/*
 * 金庸群侠传3D重制版
 * https://github.com/jynew/jynew
 *
 * 这是本开源项目文件头，所有代码均使用MIT协议。
 * 但游戏内资源和第三方插件、dll等请仔细阅读LICENSE相关授权协议文档。
 *
 * 金庸老先生千古！
 */
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using Jyx2;
using Jyx2.Middleware;
using Jyx2Configs;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class LevelMasterBooster : MonoBehaviour
{

    [LabelText("模拟移动端")] public bool m_MobileSimulate;

    [InfoBox("不允许设置所属地图，因为当前是战斗地图", InfoMessageType.Error, "@this.m_GameMap != null && this.m_IsBattleMap")]
    [LabelText("所属地图")]
    [InfoBox("仅用于在本场景启动调试时参考使用")]
    [HideIf("@this.m_IsBattleMap")]
    public Jyx2ConfigMap m_GameMap;

    [LabelText("战斗地图")] public bool m_IsBattleMap = false;
    
    GameRuntimeData runtime { get { return GameRuntimeData.Instance; } }

    private async void Awake()
    {
        await BeforeSceneLoad.loadFinishTask;

        //实例化LevelMaster
        LevelMaster levelMaster = Jyx2ResourceHelper.CreatePrefabInstance(ConStr.LevelMaster).GetComponent<LevelMaster>();
        DontDestroyOnLoad(levelMaster);
        levelMaster.name = "LevelMaster";
        levelMaster.transform.SetParent(transform, false);
        levelMaster.MobileSimulate = m_MobileSimulate;

        if (LevelMaster.GetCurrentGameMap() == null && m_GameMap != null)
        {
            LevelMaster.SetCurrentMap(m_GameMap);
        }
    }

    private async void Start()
    {
        await BeforeSceneLoad.loadFinishTask;

        if (GameRuntimeData.Instance == null)
        {
            GameRuntimeData.CreateNew();
        }

        if (m_IsBattleMap)
            return;

        //设置所有的宝箱
        foreach(var chest in GameObject.FindObjectsOfType<MapChest>())
        {
            chest.Init();
        }

        //设置所有的场景变更
        RefreshSceneObjects();


        //所有改变的物体
        foreach (var obj in FindObjectsOfType<FixWithGameRuntime>())
        {
            obj.Reload();
        }
        
        Jyx2_UIManager.Instance.ShowMainUI();
    }

    public void ReplaceSceneObject(string scene, string path, string replace)
    {
        SetSceneInfo(path, replace, scene);
    }

    private const string CONTROLLER_SCENE_INFO_PRFIX = "controller:";
    public void ReplaceNpcAnimatorController(string scene,string npc, string controllerPath)
    {
        SetSceneInfo(npc, CONTROLLER_SCENE_INFO_PRFIX + controllerPath, scene);
    }


    /// <summary>
    /// 设置场景数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="scene">如果为空，则是当前场景</param>
    void SetSceneInfo(string key, string value, string scene = "")
    {
        string sceneName = "";
        //当前场景
        if (string.IsNullOrEmpty(scene))
        {
            //sceneName = SceneManager.GetActiveScene().name;
            sceneName = LevelMaster.GetCurrentGameMap().Id.ToString();
        }
        else
        {
            sceneName = scene;
        }
        
        var dict = runtime.GetSceneInfo(sceneName);
        if (dict == null)
            dict = new Dictionary<string, string>();
        dict[key] = value;
        runtime.SetSceneInfo(sceneName, dict);

        //如果是当前场景
        if (string.IsNullOrEmpty(scene))
        {
            RefreshSceneObjects();
        }
    }

    public bool IsBattle()
    {
        var battleLoader = FindObjectOfType<BattleLoader>();
        return battleLoader != null;
    }

    public void RefreshSceneObjects()
    {
        if (IsBattle())
            return;

        if (LevelMaster.Instance == null) return;

        var currentGameMap = LevelMaster.GetCurrentGameMap();
        if (currentGameMap == null)
            return;

        string sceneName = currentGameMap.Id.ToString();
        var dict = runtime.GetSceneInfo(sceneName);
        if (dict == null)
            return;
        foreach(var kv in dict)
        {
            string objPath = "Level/" + kv.Key;

            GameObject obj = GameObject.Find(objPath);
            if (obj == null)
            {
                Debug.LogError("RefreshSceneObjects错误：找不到对象:" + objPath);
                continue;
            }

            
            //设置是否可见
            if (string.IsNullOrEmpty(kv.Value) || kv.Value.Equals("0"))
            {
                obj.SetActive(false);
            }
            else if (kv.Value.Equals("1"))
            {
                obj.SetActive(true);
            }
            else if (kv.Value.StartsWith(CONTROLLER_SCENE_INFO_PRFIX)) //设置animatorController
            {
                // 如果有animatorController则必须可见
                obj.SetActive(true);

                string animationControllerPath = kv.Value.Replace(CONTROLLER_SCENE_INFO_PRFIX, "");
                
                var animator = obj.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError($"错误：{obj.name}没有Animator组件。");
                    return;
                }

                try
                {
                    Jyx2ResourceHelper.LoadAsset<RuntimeAnimatorController>(animationControllerPath,
                        rst => { animator.runtimeAnimatorController = rst; });
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}