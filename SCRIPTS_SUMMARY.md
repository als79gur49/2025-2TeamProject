# Scripts 폴더 요약

Unity 프로젝트의 `/Assets/Script/` 폴더 내 스크립트들에 대한 종합적인 요약입니다.

## 📁 폴더 구조
```
Assets/Script/
├── TestScript.cs                     # 테스트용 스크립트
└── Audio/                           # 오디오 시스템 전용 폴더
    ├── 📋 인터페이스 (Interfaces)
    │   ├── IAudioService.cs          # 기본 오디오 서비스 인터페이스
    │   ├── IBGMAudioService.cs       # BGM 전용 서비스 인터페이스
    │   ├── IEffectAudioService.cs    # 효과음 전용 서비스 인터페이스
    │   ├── IVolumeController.cs      # 볼륨 제어 인터페이스
    │   ├── IAudioClipRepository.cs   # 오디오 클립 저장소 인터페이스
    │   └── IAudioServiceContainer.cs # 서비스 컨테이너 인터페이스
    ├── 🏗️ 구현체 (Implementations)
    │   ├── BGMAudioService.cs        # BGM 서비스 구현
    │   ├── EffectAudioService.cs     # 효과음 서비스 구현
    │   ├── VolumeController.cs       # 볼륨 제어 구현
    │   ├── AudioServiceContainer.cs  # DI 컨테이너 구현
    │   └── AudioClipRepository.cs    # 오디오 클립 저장소 구현
    ├── 📦 데이터 & 유틸리티
    │   ├── AudioClipData.cs          # 오디오 클립 데이터 구조체
    │   └── AudioServiceEvents.cs     # 전역 이벤트 시스템
    ├── 🎮 UI 통합
    │   └── ButtonSoundPlayer.cs      # 버튼 클릭 사운드 재생
    └── 🧪 테스트
        └── AudioServiceContainerTest.cs # 서비스 컨테이너 테스트
```

## 🎵 오디오 시스템 아키텍처

### 🏛️ 의존성 주입 패턴
- **AudioServiceContainer**: 모든 오디오 서비스의 생명주기 관리 및 DI 컨테이너
- **serviceRegistry**: Dictionary 기반 서비스 레지스트리로 런타임 서비스 주입 지원
- **GetService<T>()**: 제네릭 메서드를 통한 타입 안전한 서비스 접근

### 🎼 서비스 계층
1. **BGMAudioService**: 단일 BGM 재생, 페이드, 크로스페이드
2. **EffectAudioService**: 다중 효과음 재생, 동시 재생 관리
3. **VolumeController**: AudioMixer 기반 볼륨 제어, PlayerPrefs 저장

### 💾 데이터 관리
- **AudioClipRepository**: ScriptableObject 기반 오디오 클립 관리
- **AudioClipData**: 오디오 클립 메타데이터 (이름, 볼륨, 피치)

## 📝 주요 스크립트 상세

### 🎯 AudioServiceContainer.cs
**역할**: 중앙 집중식 오디오 서비스 관리자
```csharp
// 서비스 접근
var bgm = AudioServiceContainer.Instance.GetService<IBGMAudioService>();
var effects = AudioServiceContainer.Instance.GetService<IEffectAudioService>();
var volume = AudioServiceContainer.Instance.GetService<IVolumeController>();
```
- Singleton 패턴으로 전역 접근 가능
- DontDestroyOnLoad로 씬 전환 시에도 유지
- 자동 서비스 생성 및 지연 초기화 지원

### 🎼 BGMAudioService.cs
**기능**: 배경음악 전용 서비스
- 단일 BGM 재생 (AudioSource 1개)
- 페이드 인/아웃, 크로스페이드
- AudioMixer 그룹 연동
- Repository 패턴으로 클립 관리

### 🔊 EffectAudioService.cs  
**기능**: 효과음 전용 서비스
- 다중 효과음 동시 재생 (AudioSource 풀링)
- 3D 사운드 지원 (위치 기반)
- 효과음 우선순위 관리
- 메모리 효율적인 AudioSource 재사용

### 🎚️ VolumeController.cs
**기능**: 볼륨 제어 및 설정 관리
- Master/BGM/Effect 독립적 볼륨 제어
- dB 단위 ↔ 정규화(0~1) 변환
- 음소거 기능 및 상태 복원
- PlayerPrefs 기반 설정 영구 저장

### 🎮 ButtonSoundPlayer.cs
**기능**: UI 버튼 사운드 통합
- Button.OnClick() 자동 연결
- 6가지 버튼 타입별 사운드 (Default, Confirm, Cancel, etc.)
- 호버 사운드 지원 (EventTrigger)
- 런타임 사운드 설정 변경 가능

## 🔧 사용 방법

### 기본 설정
1. AudioServiceContainer 프리팹을 씬에 배치
2. AudioMixer 에셋 연결
3. AudioClipRepository에 사운드 클립 등록

### BGM 재생
```csharp
var bgmService = AudioServiceContainer.Instance.GetService<IBGMAudioService>();
bgmService.PlayBGM("MainTheme", loop: true);
bgmService.CrossFadeBGM("BattleTheme", 2.0f);
```

### 효과음 재생
```csharp
var effectService = AudioServiceContainer.Instance.GetService<IEffectAudioService>();
effectService.PlayEffect("ButtonClick");
effectService.PlayEffect3D("Explosion", transform.position);
```

### 볼륨 제어
```csharp
var volumeController = AudioServiceContainer.Instance.GetService<IVolumeController>();
volumeController.SetMasterVolumeNormalized(0.8f); // UI 슬라이더용
volumeController.MuteAll(true); // 전체 음소거
```

### 버튼 사운드
```csharp
// 1. ButtonSoundPlayer 스크립트를 Button GameObject에 추가
// 2. Inspector에서 clickSoundName 설정
// 3. 자동으로 OnClick 이벤트에 연결됨
```

## 🧪 테스트 및 디버깅

### AudioServiceContainerTest.cs
- 자동화된 통합 테스트
- Mock 서비스를 통한 DI 컨테이너 검증
- 서비스 초기화 및 생명주기 테스트
- GetService<T>() 메서드 동작 확인

### 디버그 기능
```csharp
// 서비스 상태 확인
AudioServiceContainer.Instance.LogInitializationStatus();

// 오디오 상태 요약
string status = AudioServiceContainer.Instance.GetAudioStatusSummary();
Debug.Log(status);
```

## 🎨 설계 패턴

1. **의존성 주입 (DI)**: serviceRegistry 기반 서비스 관리
2. **Singleton**: AudioServiceContainer 전역 접근
3. **Factory**: AudioServiceFactory를 통한 서비스 생성
4. **Repository**: AudioClipRepository를 통한 데이터 관리
5. **Observer**: AudioServiceEvents 전역 이벤트 시스템
6. **Strategy**: 서비스별 특화된 오디오 재생 전략

## 🚀 확장성

- **새로운 오디오 서비스**: IAudioService 구현하여 serviceRegistry에 등록
- **커스텀 볼륨 타입**: VolumeType enum 확장
- **새로운 버튼 사운드**: ButtonSoundType enum 추가
- **테스트 확장**: Mock 서비스 패턴을 통한 단위 테스트 작성

---

**총 스크립트 수**: 16개  
**주요 기능**: 통합 오디오 시스템, DI 컨테이너, UI 사운드 지원  
**설계 철학**: 모듈화, 테스트 가능성, 확장성, 타입 안전성  