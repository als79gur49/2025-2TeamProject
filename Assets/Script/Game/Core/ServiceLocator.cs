using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// 개선된 서비스 로케이터 - 의존성 주입을 위한 중앙 서비스 관리
    /// FindObjectOfType를 대체하여 성능과 결합도를 개선
    /// </summary>
    public static class ServiceLocator
    {
        // ✅ 서비스 저장소
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, object> singletonInstances = new Dictionary<Type, object>();
        private static readonly object lockObject = new object();
        private static bool isInitialized = false;

        // ✅ 초기화 이벤트
        public static event Action OnServicesCleared;
        public static event Action<Type, object> OnServiceRegistered;
        public static event Action<Type> OnServiceUnregistered;

        /// <summary>
        /// 서비스 등록 - 인터페이스와 구현체 매핑
        /// </summary>
        public static void Register<TInterface>(TInterface implementation) where TInterface : class
        {
            Register(typeof(TInterface), implementation);
        }

        /// <summary>
        /// 서비스 등록 - 타입으로 등록
        /// </summary>
        public static void Register(Type serviceType, object implementation)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            
            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            if (!serviceType.IsAssignableFrom(implementation.GetType()))
                throw new ArgumentException($"Implementation {implementation.GetType().Name} does not implement {serviceType.Name}");

            lock (lockObject)
            {
                services[serviceType] = implementation;
                OnServiceRegistered?.Invoke(serviceType, implementation);
                
                Debug.Log($"[ServiceLocator] Registered {serviceType.Name} -> {implementation.GetType().Name}");
            }
        }

        /// <summary>
        /// 싱글톤 서비스 등록 - MonoBehaviour 컴포넌트용
        /// </summary>
        public static void RegisterSingleton<T>(T instance) where T : MonoBehaviour
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            lock (lockObject)
            {
                var type = typeof(T);
                singletonInstances[type] = instance;
                services[type] = instance;
                
                // 해당 오브젝트가 파괴될 때 자동으로 등록 해제
                if (instance != null)
                {
                    var gameObject = instance.gameObject;
                    if (gameObject != null)
                    {
                        var cleanup = gameObject.GetComponent<ServiceCleanup>();
                        if (cleanup == null)
                        {
                            cleanup = gameObject.AddComponent<ServiceCleanup>();
                        }
                        cleanup.RegisterType(type);
                    }
                }
                
                OnServiceRegistered?.Invoke(type, instance);
                Debug.Log($"[ServiceLocator] Registered Singleton {type.Name} -> {instance.name}");
            }
        }

        /// <summary>
        /// 서비스 조회 - 제네릭 버전
        /// </summary>
        public static T Get<T>() where T : class
        {
            return Get(typeof(T)) as T;
        }

        /// <summary>
        /// 서비스 조회 - 타입 버전
        /// </summary>
        public static object Get(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            lock (lockObject)
            {
                if (services.TryGetValue(serviceType, out var service))
                {
                    // MonoBehaviour 싱글톤이 파괴되었는지 확인
                    if (service is MonoBehaviour mono && mono == null)
                    {
                        services.Remove(serviceType);
                        singletonInstances.Remove(serviceType);
                        Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} was destroyed, removing from registry");
                        return null;
                    }
                    
                    return service;
                }

                Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} not found. Available services: {string.Join(", ", services.Keys)}");
                return null;
            }
        }

        /// <summary>
        /// 안전한 서비스 조회 - null 체크 포함
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// 서비스 등록 여부 확인
        /// </summary>
        public static bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// 서비스 등록 여부 확인 - 타입 버전
        /// </summary>
        public static bool IsRegistered(Type serviceType)
        {
            if (serviceType == null) return false;

            lock (lockObject)
            {
                if (!services.ContainsKey(serviceType)) return false;
                
                // MonoBehaviour 서비스가 파괴되었는지 확인
                var service = services[serviceType];
                if (service is MonoBehaviour mono && mono == null)
                {
                    services.Remove(serviceType);
                    singletonInstances.Remove(serviceType);
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// 서비스 등록 해제
        /// </summary>
        public static void Unregister<T>()
        {
            Unregister(typeof(T));
        }

        /// <summary>
        /// 서비스 등록 해제 - 타입 버전
        /// </summary>
        public static void Unregister(Type serviceType)
        {
            if (serviceType == null) return;

            lock (lockObject)
            {
                if (services.Remove(serviceType))
                {
                    singletonInstances.Remove(serviceType);
                    OnServiceUnregistered?.Invoke(serviceType);
                    Debug.Log($"[ServiceLocator] Unregistered {serviceType.Name}");
                }
            }
        }

        /// <summary>
        /// 모든 서비스 정리
        /// </summary>
        public static void Clear()
        {
            lock (lockObject)
            {
                services.Clear();
                singletonInstances.Clear();
                isInitialized = false;
                OnServicesCleared?.Invoke();
                Debug.Log("[ServiceLocator] All services cleared");
            }
        }

        /// <summary>
        /// 모든 서비스 등록 해제 (Phase 3 호환성)
        /// </summary>
        public static void UnregisterAll()
        {
            Clear();
        }

        /// <summary>
        /// 초기화 여부 확인
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// 초기화 마크
        /// </summary>
        public static void MarkAsInitialized()
        {
            isInitialized = true;
            Debug.Log("[ServiceLocator] Marked as initialized");
        }

        /// <summary>
        /// 등록된 서비스 목록 반환 (디버깅용)
        /// </summary>
        public static IReadOnlyDictionary<Type, object> GetRegisteredServices()
        {
            lock (lockObject)
            {
                return new Dictionary<Type, object>(services);
            }
        }

        /// <summary>
        /// 서비스 상태 검증 (파괴된 MonoBehaviour 정리)
        /// </summary>
        public static void ValidateServices()
        {
            lock (lockObject)
            {
                var toRemove = new List<Type>();
                
                foreach (var kvp in services)
                {
                    if (kvp.Value is MonoBehaviour mono && mono == null)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var type in toRemove)
                {
                    services.Remove(type);
                    singletonInstances.Remove(type);
                    Debug.LogWarning($"[ServiceLocator] Removed destroyed service: {type.Name}");
                }
            }
        }

        /// <summary>
        /// 조건부 서비스 생성 및 등록
        /// </summary>
        public static T GetOrCreate<T>() where T : class, new()
        {
            var service = Get<T>();
            if (service == null)
            {
                service = new T();
                Register<T>(service);
                Debug.Log($"[ServiceLocator] Created and registered new instance of {typeof(T).Name}");
            }
            return service;
        }

        // ✅ 런타임 상태 모니터링
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            // 도메인 재로드 시 정적 데이터 초기화 (Unity Editor용)
            Clear();
        }
    }

    /// <summary>
    /// MonoBehaviour 서비스 자동 정리 컴포넌트
    /// </summary>
    internal class ServiceCleanup : MonoBehaviour
    {
        private readonly List<Type> registeredTypes = new List<Type>();

        public void RegisterType(Type type)
        {
            if (!registeredTypes.Contains(type))
            {
                registeredTypes.Add(type);
            }
        }

        private void OnDestroy()
        {
            foreach (var type in registeredTypes)
            {
                ServiceLocator.Unregister(type);
            }
            registeredTypes.Clear();
        }
    }

    /// <summary>
    /// 서비스 로케이터 확장 메서드
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// MonoBehaviour에서 서비스 의존성 주입
        /// </summary>
        public static void InjectDependencies(this MonoBehaviour component)
        {
            var type = component.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var injectAttribute = field.GetCustomAttributes(typeof(InjectAttribute), false);
                if (injectAttribute.Length > 0)
                {
                    var service = ServiceLocator.Get(field.FieldType);
                    if (service != null)
                    {
                        field.SetValue(component, service);
                        Debug.Log($"[ServiceLocator] Injected {field.FieldType.Name} into {type.Name}.{field.Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ServiceLocator] Failed to inject {field.FieldType.Name} into {type.Name}.{field.Name} - service not registered");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 의존성 주입 마킹 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public bool Required { get; set; } = true;
        
        public InjectAttribute(bool required = true)
        {
            Required = required;
        }
    }
