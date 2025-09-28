/**
 * Claude Code Terminal Status Bar Implementation
 * 터미널 하단에 토큰 사용량과 세션 리셋 시간을 표시하는 상태바
 */

class ClaudeStatusBar {
  constructor() {
    this.data = {
      tokensUsed: 0,
      tokensTotal: 20000, // Default Claude limit
      resetTime: this.getNextResetTime(),
      sessionStart: new Date(),
      apiStatus: 'connected'
    };

    this.statusBarElement = null;
    this.updateInterval = null;
    this.init();
  }

  init() {
    this.createStatusBar();
    this.startTimer();
    this.loadStoredData();
  }

  createStatusBar() {
    // 기존 상태바가 있다면 제거
    const existing = document.getElementById('claude-status-bar');
    if (existing) existing.remove();

    // 상태바 컨테이너 생성
    this.statusBarElement = document.createElement('div');
    this.statusBarElement.id = 'claude-status-bar';
    this.statusBarElement.className = 'claude-status-bar';

    // CSS 스타일 추가
    this.addStyles();

    // 터미널 컨테이너에 추가
    const terminalContainer = document.querySelector('.terminal-container') || document.body;
    terminalContainer.appendChild(this.statusBarElement);

    this.render();
  }

  addStyles() {
    const styleId = 'claude-statusbar-styles';
    if (document.getElementById(styleId)) return;

    const styles = document.createElement('style');
    styles.id = styleId;
    styles.textContent = `
      .claude-status-bar {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        background: #1e1e1e;
        border-top: 1px solid #333;
        padding: 6px 12px;
        font-family: 'Monaco', 'Menlo', 'Consolas', monospace;
        font-size: 11px;
        color: #cccccc;
        display: flex;
        justify-content: space-between;
        align-items: center;
        z-index: 1000;
        box-shadow: 0 -2px 8px rgba(0,0,0,0.3);
      }

      .status-left {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .token-usage {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .progress-bar {
        width: 120px;
        height: 4px;
        background: #333;
        border-radius: 2px;
        overflow: hidden;
        border: 1px solid #444;
      }

      .progress-fill {
        height: 100%;
        transition: width 0.3s ease, background-color 0.3s ease;
        border-radius: 1px;
      }

      .progress-fill.safe {
        background: linear-gradient(90deg, #4caf50, #66bb6a);
      }
      .progress-fill.warning {
        background: linear-gradient(90deg, #ff9800, #ffb74d);
      }
      .progress-fill.danger {
        background: linear-gradient(90deg, #f44336, #ef5350);
      }

      .token-text {
        font-variant-numeric: tabular-nums;
        min-width: 80px;
      }

      .status-right {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .reset-timer {
        font-variant-numeric: tabular-nums;
        color: #81c784;
      }

      .session-info {
        color: #64b5f6;
        font-size: 10px;
      }

      .api-status {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #4caf50;
        box-shadow: 0 0 4px rgba(76, 175, 80, 0.5);
      }

      .api-status.disconnected {
        background: #f44336;
        box-shadow: 0 0 4px rgba(244, 67, 54, 0.5);
      }

      .api-status.error {
        background: #ff9800;
        box-shadow: 0 0 4px rgba(255, 152, 0, 0.5);
      }

      @media (max-width: 768px) {
        .claude-status-bar {
          font-size: 10px;
          padding: 4px 8px;
        }
        .progress-bar {
          width: 80px;
        }
        .session-info {
          display: none;
        }
      }
    `;
    document.head.appendChild(styles);
  }

  render() {
    if (!this.statusBarElement) return;

    const usagePercent = (this.data.tokensUsed / this.data.tokensTotal) * 100;
    const progressClass = usagePercent >= 90 ? 'danger' : usagePercent >= 70 ? 'warning' : 'safe';

    this.statusBarElement.innerHTML = `
      <div class="status-left">
        <div class="token-usage">
          <span class="token-text">${this.formatTokens(this.data.tokensUsed)}/${this.formatTokens(this.data.tokensTotal)}</span>
          <div class="progress-bar">
            <div class="progress-fill ${progressClass}" style="width: ${Math.min(usagePercent, 100)}%"></div>
          </div>
          <span style="color: ${progressClass === 'danger' ? '#f44336' : progressClass === 'warning' ? '#ff9800' : '#4caf50'}">(${usagePercent.toFixed(1)}%)</span>
        </div>
      </div>
      <div class="status-right">
        <div class="session-info">Session: ${this.formatDuration(Date.now() - this.data.sessionStart.getTime())}</div>
        <div class="reset-timer">Reset: ${this.formatTimeRemaining()}</div>
        <div class="api-status ${this.data.apiStatus}" title="API Status: ${this.data.apiStatus}"></div>
      </div>
    `;
  }

  formatTokens(tokens) {
    if (tokens >= 1000) {
      return (tokens / 1000).toFixed(1) + 'K';
    }
    return tokens.toString();
  }

  formatTimeRemaining() {
    const now = new Date();
    const diff = this.data.resetTime.getTime() - now.getTime();

    if (diff <= 0) {
      this.data.resetTime = this.getNextResetTime();
      this.data.tokensUsed = 0; // 자동 리셋
      return '00:00:00';
    }

    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    if (hours > 0) {
      return `${hours}h ${minutes}m ${seconds}s`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    } else {
      return `${seconds}s`;
    }
  }

  formatDuration(ms) {
    const hours = Math.floor(ms / (1000 * 60 * 60));
    const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60));

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  }

  getNextResetTime() {
    // Claude의 일일 리셋은 UTC 기준 자정 (한국시간 오전 9시)
    const now = new Date();
    const resetTime = new Date();
    resetTime.setUTCHours(0, 0, 0, 0);
    resetTime.setUTCDate(resetTime.getUTCDate() + 1);

    return resetTime;
  }

  updateTokenUsage(used, total = this.data.tokensTotal) {
    this.data.tokensUsed = used;
    this.data.tokensTotal = total;
    this.saveData();
    this.render();
  }

  updateApiStatus(status) {
    this.data.apiStatus = status;
    this.render();
  }

  startTimer() {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
    }

    this.updateInterval = setInterval(() => {
      this.render();
    }, 1000);
  }

  saveData() {
    const dataToSave = {
      tokensUsed: this.data.tokensUsed,
      tokensTotal: this.data.tokensTotal,
      sessionStart: this.data.sessionStart.toISOString(),
      resetTime: this.data.resetTime.toISOString()
    };
    localStorage.setItem('claude-statusbar-data', JSON.stringify(dataToSave));
  }

  loadStoredData() {
    try {
      const stored = localStorage.getItem('claude-statusbar-data');
      if (stored) {
        const data = JSON.parse(stored);

        // 같은 세션인지 확인 (1시간 이내)
        const sessionStart = new Date(data.sessionStart);
        const now = new Date();
        if (now.getTime() - sessionStart.getTime() < 60 * 60 * 1000) {
          this.data.tokensUsed = data.tokensUsed || 0;
          this.data.tokensTotal = data.tokensTotal || 20000;
          this.data.sessionStart = sessionStart;
        }

        // 리셋 시간이 지났는지 확인
        const resetTime = new Date(data.resetTime);
        if (now >= resetTime) {
          this.data.tokensUsed = 0;
          this.data.resetTime = this.getNextResetTime();
        } else {
          this.data.resetTime = resetTime;
        }
      }
    } catch (error) {
      console.warn('Failed to load status bar data:', error);
    }
  }

  destroy() {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
    }
    if (this.statusBarElement) {
      this.statusBarElement.remove();
    }
    const styles = document.getElementById('claude-statusbar-styles');
    if (styles) {
      styles.remove();
    }
  }

  // 토큰 사용량을 수동으로 증가시키는 메서드 (테스트용)
  incrementTokens(amount = 100) {
    this.updateTokenUsage(Math.min(this.data.tokensUsed + amount, this.data.tokensTotal));
  }

  // 설정 업데이트 메서드
  updateSettings(settings) {
    if (settings.tokensTotal) {
      this.data.tokensTotal = settings.tokensTotal;
    }
    if (settings.resetTime) {
      this.data.resetTime = new Date(settings.resetTime);
    }
    this.saveData();
    this.render();
  }
}

// 전역 인스턴스 생성
let claudeStatusBar = null;

// DOM이 로드되면 상태바 초기화
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    claudeStatusBar = new ClaudeStatusBar();
  });
} else {
  claudeStatusBar = new ClaudeStatusBar();
}

// Claude API 호출을 가로채서 토큰 사용량 추적
if (typeof window !== 'undefined') {
  // Fetch API 가로채기
  const originalFetch = window.fetch;
  window.fetch = function(...args) {
    return originalFetch.apply(this, args).then(response => {
      // Claude API 응답에서 토큰 사용량 추출
      if (args[0] && args[0].includes('claude') && claudeStatusBar) {
        response.clone().json().then(data => {
          if (data.usage && data.usage.input_tokens) {
            const totalTokens = (data.usage.input_tokens || 0) + (data.usage.output_tokens || 0);
            claudeStatusBar.incrementTokens(totalTokens);
          }
        }).catch(() => {
          // 무시 (JSON이 아닌 응답)
        });
      }
      return response;
    });
  };

  // 전역 접근을 위한 함수들
  window.claudeStatusBar = {
    updateTokens: (used, total) => claudeStatusBar?.updateTokenUsage(used, total),
    updateStatus: (status) => claudeStatusBar?.updateApiStatus(status),
    increment: (amount) => claudeStatusBar?.incrementTokens(amount),
    reset: () => claudeStatusBar?.updateTokenUsage(0),
    destroy: () => claudeStatusBar?.destroy()
  };
}

// 콘솔에서 사용할 수 있는 명령어들
console.log(`
Claude Status Bar 활성화됨!

사용 가능한 명령어:
- claudeStatusBar.updateTokens(used, total) - 토큰 사용량 업데이트
- claudeStatusBar.increment(100) - 토큰 사용량 증가 (테스트용)
- claudeStatusBar.reset() - 토큰 사용량 리셋
- claudeStatusBar.updateStatus('connected') - API 상태 업데이트
- claudeStatusBar.destroy() - 상태바 제거
`);