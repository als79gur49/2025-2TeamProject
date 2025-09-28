# Claude Code Terminal Status Bar Design

## 요구사항
- 터미널 입력창 아래에 상태 정보 표시
- 실시간 토큰 사용량 및 남은 토큰 수 표시
- 세션 리셋까지 남은 시간 표시
- 시각적으로 직관적인 프로그레스 바 형태

## 상태바 레이아웃

```
┌─────────────────────────────────────────────────────────────────┐
│ > Your prompt here...                                           │
└─────────────────────────────────────────────────────────────────┘
┌─ Status ────────────────────────────────────────────────────────┐
│ Tokens: ████████████░░░░ 12.5K/20K (62%) │ Reset: 2h 15m 30s   │
└─────────────────────────────────────────────────────────────────┘
```

## 상태바 컴포넌트

### 1. 토큰 사용량 표시기
- 프로그레스 바: `████████████░░░░`
- 텍스트 정보: `12.5K/20K (62%)`
- 색상 코딩:
  - 녹색: 0-70% 사용
  - 노랑: 70-90% 사용
  - 빨강: 90-100% 사용

### 2. 세션 리셋 타이머
- 형식: `Reset: 2h 15m 30s`
- 실시간 업데이트 (매초)
- 1시간 미만시: `Reset: 45m 30s`
- 1분 미만시: `Reset: 30s`

### 3. 추가 상태 정보 (옵션)
- 현재 모델: `Model: Claude-3.5-Sonnet`
- 세션 지속시간: `Session: 1h 23m`
- API 상태: `Status: Connected`

## 구현 방안

### CLI 인터페이스 통합
```typescript
interface StatusBarData {
  tokensUsed: number;
  tokensTotal: number;
  resetTime: Date;
  modelName: string;
  sessionStart: Date;
  apiStatus: 'connected' | 'disconnected' | 'error';
}

class TerminalStatusBar {
  private data: StatusBarData;
  private updateInterval: NodeJS.Timeout;

  constructor(container: HTMLElement) {
    this.setupStatusBar(container);
    this.startTimer();
  }

  updateTokenUsage(used: number, total: number): void {
    this.data.tokensUsed = used;
    this.data.tokensTotal = total;
    this.render();
  }

  private render(): void {
    // 상태바 렌더링 로직
  }

  private formatTimeRemaining(): string {
    const now = new Date();
    const diff = this.data.resetTime.getTime() - now.getTime();
    // 시간 포맷팅 로직
  }
}
```

### 스타일링
```css
.claude-status-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #1e1e1e;
  border-top: 1px solid #333;
  padding: 8px 16px;
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 12px;
  color: #cccccc;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.token-usage {
  display: flex;
  align-items: center;
  gap: 8px;
}

.progress-bar {
  width: 200px;
  height: 6px;
  background: #333;
  border-radius: 3px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  transition: width 0.3s ease;
}

.progress-fill.safe { background: #4caf50; }
.progress-fill.warning { background: #ff9800; }
.progress-fill.danger { background: #f44336; }

.reset-timer {
  font-variant-numeric: tabular-nums;
}
```

## 데이터 소스

### 1. 토큰 사용량 추적
- Claude API 응답에서 토큰 사용량 추출
- 로컬 스토리지에 누적 사용량 저장
- 세션별 사용량 리셋 관리

### 2. 세션 리셋 시간
- Claude의 일일/월별 한도 리셋 시간
- 사용자 설정 가능한 리셋 주기
- 타임존 고려한 정확한 시간 계산

### 3. 실시간 업데이트
- WebSocket 또는 폴링을 통한 실시간 데이터
- 토큰 사용량 변화 감지
- 네트워크 상태 모니터링

## 구현 우선순위

1. **기본 상태바 컨테이너** - 터미널 하단 고정 영역
2. **토큰 사용량 표시** - 프로그레스 바 + 텍스트
3. **리셋 타이머** - 실시간 카운트다운
4. **스타일링** - 다크 테마 통합
5. **데이터 연동** - Claude API 통합
6. **설정 옵션** - 사용자 커스터마이징

## 사용자 경험

### 시각적 피드백
- 토큰 사용량 증가시 애니메이션
- 한도 근접시 경고 색상
- 리셋 임박시 알림 표시

### 상호작용
- 상태바 클릭시 상세 정보 팝업
- 우클릭으로 설정 메뉴 접근
- 토글 가능한 표시/숨김 기능

이 설계를 바탕으로 Claude Code 터미널에 통합할 수 있는 상태바 기능을 구현할 수 있습니다.