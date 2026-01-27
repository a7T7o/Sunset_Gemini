using System.Collections;
using UnityEngine;

namespace Sunset.Events
{
    /// <summary>
    /// EventBus运行器
    /// 用于处理延迟发布和协程相关功能
    /// </summary>
    public class EventBusRunner : MonoBehaviour
    {
        private static EventBusRunner _instance;
        
        public static EventBusRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[EventBusRunner]");
                    _instance = go.AddComponent<EventBusRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        /// <summary>
        /// 下一帧发布事件
        /// </summary>
        public void PublishNextFrame<T>(T eventData) where T : IGameEvent
        {
            StartCoroutine(PublishNextFrameCoroutine(eventData));
        }
        
        private IEnumerator PublishNextFrameCoroutine<T>(T eventData) where T : IGameEvent
        {
            yield return null;
            EventBus.Publish(eventData);
        }
        
        /// <summary>
        /// 延迟发布事件
        /// </summary>
        public void PublishAfterDelay<T>(T eventData, float delay) where T : IGameEvent
        {
            StartCoroutine(PublishAfterDelayCoroutine(eventData, delay));
        }
        
        private IEnumerator PublishAfterDelayCoroutine<T>(T eventData, float delay) where T : IGameEvent
        {
            yield return new WaitForSeconds(delay);
            EventBus.Publish(eventData);
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
