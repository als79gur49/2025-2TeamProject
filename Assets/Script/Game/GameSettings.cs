using UnityEngine;

namespace Game
{
    /// <summary>
    /// 전역 게임 설정 관리
    /// 애니메이션, VFX, 성능 관련 설정을 중앙에서 관리
    /// </summary>
    public static class GameSettings
    {
        #region Animation Settings

        private const string PREF_ANIMATION_SPEED = "Game.AnimationSpeed";
        private const string PREF_ENABLE_ANIMATIONS = "Game.EnableAnimations";
        private const string PREF_ENABLE_VFX = "Game.EnableVFX";
        private const string PREF_ENABLE_SFX = "Game.EnableSFX";

        private static float _globalAnimationSpeed = 1.0f;
        private static bool _enableUnitAnimations = true;
        private static bool _enableVFX = true;
        private static bool _enableSFX = true;

        /// <summary>
        /// 전역 애니메이션 속도 배율 (0.1 ~ 3.0)
        /// 1.0 = 기본 속도, 2.0 = 2배속, 0.5 = 반속
        /// </summary>
        public static float GlobalAnimationSpeed
        {
            get => _globalAnimationSpeed;
            set
            {
                _globalAnimationSpeed = Mathf.Clamp(value, 0.1f, 3.0f);
                PlayerPrefs.SetFloat(PREF_ANIMATION_SPEED, _globalAnimationSpeed);
                PlayerPrefs.Save();
                OnAnimationSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// 유닛 애니메이션 활성화 여부
        /// false = 애니메이션 스킵 (즉시 실행)
        /// </summary>
        public static bool EnableUnitAnimations
        {
            get => _enableUnitAnimations;
            set
            {
                _enableUnitAnimations = value;
                PlayerPrefs.SetInt(PREF_ENABLE_ANIMATIONS, value ? 1 : 0);
                PlayerPrefs.Save();
                OnAnimationSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// VFX(파티클 효과) 활성화 여부
        /// </summary>
        public static bool EnableVFX
        {
            get => _enableVFX;
            set
            {
                _enableVFX = value;
                PlayerPrefs.SetInt(PREF_ENABLE_VFX, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// SFX(사운드 효과) 활성화 여부
        /// </summary>
        public static bool EnableSFX
        {
            get => _enableSFX;
            set
            {
                _enableSFX = value;
                PlayerPrefs.SetInt(PREF_ENABLE_SFX, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Performance Settings

        private const string PREF_TARGET_FPS = "Game.TargetFPS";
        private const string PREF_VSYNC = "Game.VSync";

        private static int _targetFPS = 60;
        private static bool _vSyncEnabled = true;

        /// <summary>
        /// 목표 FPS (30, 60, 120, 무제한=-1)
        /// </summary>
        public static int TargetFPS
        {
            get => _targetFPS;
            set
            {
                _targetFPS = value;
                Application.targetFrameRate = value;
                PlayerPrefs.SetInt(PREF_TARGET_FPS, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// VSync 활성화 여부
        /// </summary>
        public static bool VSyncEnabled
        {
            get => _vSyncEnabled;
            set
            {
                _vSyncEnabled = value;
                QualitySettings.vSyncCount = value ? 1 : 0;
                PlayerPrefs.SetInt(PREF_VSYNC, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 애니메이션 설정 변경 이벤트
        /// UnitAnimationController가 구독하여 설정 반영
        /// </summary>
        public static event System.Action OnAnimationSettingsChanged;

        #endregion

        #region Initialization

        /// <summary>
        /// 게임 시작 시 설정 로드
        /// [RuntimeInitializeOnLoadMethod] 속성으로 자동 호출
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoadSettings();
            Debug.Log($"[GameSettings] Initialized - Animation Speed: {_globalAnimationSpeed}x, " +
                     $"Animations: {(_enableUnitAnimations ? "ON" : "OFF")}, " +
                     $"VFX: {(_enableVFX ? "ON" : "OFF")}");
        }

        /// <summary>
        /// PlayerPrefs에서 설정 로드
        /// </summary>
        private static void LoadSettings()
        {
            // Animation Settings
            _globalAnimationSpeed = PlayerPrefs.GetFloat(PREF_ANIMATION_SPEED, 1.0f);
            _enableUnitAnimations = PlayerPrefs.GetInt(PREF_ENABLE_ANIMATIONS, 1) == 1;
            _enableVFX = PlayerPrefs.GetInt(PREF_ENABLE_VFX, 1) == 1;
            _enableSFX = PlayerPrefs.GetInt(PREF_ENABLE_SFX, 1) == 1;

            // Performance Settings
            _targetFPS = PlayerPrefs.GetInt(PREF_TARGET_FPS, 60);
            _vSyncEnabled = PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;

            // Apply settings
            Application.targetFrameRate = _targetFPS;
            QualitySettings.vSyncCount = _vSyncEnabled ? 1 : 0;
        }

        /// <summary>
        /// 모든 설정을 기본값으로 리셋
        /// </summary>
        public static void ResetToDefaults()
        {
            GlobalAnimationSpeed = 1.0f;
            EnableUnitAnimations = true;
            EnableVFX = true;
            EnableSFX = true;
            TargetFPS = 60;
            VSyncEnabled = true;

            Debug.Log("[GameSettings] Reset to defaults");
        }

        #endregion

        #region Preset Profiles

        /// <summary>
        /// 빠른 플레이 프리셋 (애니메이션 2배속)
        /// </summary>
        public static void ApplyFastPlayPreset()
        {
            GlobalAnimationSpeed = 2.0f;
            EnableUnitAnimations = true;
            EnableVFX = true;
            Debug.Log("[GameSettings] Applied Fast Play preset");
        }

        /// <summary>
        /// 초고속 플레이 프리셋 (애니메이션 스킵)
        /// </summary>
        public static void ApplySpeedRunPreset()
        {
            GlobalAnimationSpeed = 1.0f;
            EnableUnitAnimations = false;
            EnableVFX = false;
            Debug.Log("[GameSettings] Applied Speed Run preset");
        }

        /// <summary>
        /// 성능 우선 프리셋 (효과 최소화)
        /// </summary>
        public static void ApplyPerformancePreset()
        {
            GlobalAnimationSpeed = 1.5f;
            EnableUnitAnimations = true;
            EnableVFX = false;
            TargetFPS = 30;
            Debug.Log("[GameSettings] Applied Performance preset");
        }

        /// <summary>
        /// 품질 우선 프리셋 (모든 효과 활성화)
        /// </summary>
        public static void ApplyQualityPreset()
        {
            GlobalAnimationSpeed = 1.0f;
            EnableUnitAnimations = true;
            EnableVFX = true;
            EnableSFX = true;
            TargetFPS = 60;
            VSyncEnabled = true;
            Debug.Log("[GameSettings] Applied Quality preset");
        }

        #endregion
    }
}
