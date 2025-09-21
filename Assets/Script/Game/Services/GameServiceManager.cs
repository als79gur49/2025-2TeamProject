using UnityEngine;
using Game.Services;
using Game.Core;

namespace Game.Services
{
    /// <summary>
    /// Manages service initialization order and coordination
    /// </summary>
    public class GameServiceManager : MonoBehaviour
    {
        [Header("Service Components")]
        [SerializeField] private TurnService turnService;
        [SerializeField] private UnitService unitService;
        [SerializeField] private UIService uiService;
        [SerializeField] private GameService gameService;
        
        [Header("Auto-Initialize")]
        [SerializeField] private bool autoInitialize = true;
        
        private bool isInitialized = false;
        
        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeServices();
            }
        }
        
        public void InitializeServices()
        {           
            // Ensure services are present
            EnsureServiceComponents();
            
            // Services will auto-register through their Awake() methods
            // and auto-initialize through their Start() methods
            
            isInitialized = true;
        }
        
        private void EnsureServiceComponents()
        {
            
            if (turnService == null)
            {
                turnService = gameObject.AddComponent<TurnService>();
            }
            
            if (unitService == null)
            {
                unitService = gameObject.AddComponent<UnitService>();
            }
            
            if (uiService == null)
            {
                uiService = gameObject.AddComponent<UIService>();
            }
            
            if (gameService == null)
            {
                gameService = gameObject.AddComponent<GameService>();
            }
        }
    }
}