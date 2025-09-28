#!/bin/bash

# CardData 리팩토링 백업 및 브랜치 생성 스크립트
#
# 이 스크립트는 CardData 리팩토링 계획서의 Phase 1.1을 실행합니다:
# - 현재 CardData 시스템 백업
# - feature/carddata-refactoring 브랜치 생성

set -e  # 에러 발생 시 스크립트 종료

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 로그 함수
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 실행 시작
echo "============================================"
echo "CardData 리팩토링 백업 및 브랜치 생성 스크립트"
echo "============================================"
echo

# 1. Git 저장소 확인
log_info "Git 저장소 상태 확인 중..."
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    log_error "Git 저장소가 아닙니다. Git 프로젝트 루트에서 실행해주세요."
    exit 1
fi

# 2. 현재 브랜치 확인
CURRENT_BRANCH=$(git branch --show-current)
log_info "현재 브랜치: $CURRENT_BRANCH"

# 3. 백업 디렉토리 생성
BACKUP_DIR="backup_carddata_$(date +%Y%m%d_%H%M%S)"
log_info "백업 디렉토리 생성: $BACKUP_DIR"
mkdir -p "$BACKUP_DIR"

# 4. CardData 관련 파일 백업
log_info "CardData 관련 파일들을 백업 중..."

# CardData 관련 파일 목록 정의
CARDDATA_FILES=(
    "CardData_Phase1_Test.cs"
    "Phase3_Data_Migration_Test.cs"
    "CardData_Refactoring_Plan.md"
    "CardData_Refactoring_Plan_v2.md"
)

# Assets 디렉토리에서 CardData 관련 파일들 찾기
if [ -d "Assets" ]; then
    log_info "Assets 디렉토리에서 CardData 관련 파일 검색 중..."
    find Assets -name "*CardData*" -type f | while read -r file; do
        if [ -f "$file" ]; then
            # 디렉토리 구조 유지하며 복사
            DEST_DIR="$BACKUP_DIR/$(dirname "$file")"
            mkdir -p "$DEST_DIR"
            cp "$file" "$DEST_DIR/"
            log_info "백업됨: $file"
        fi
    done
fi

# 루트 디렉토리의 CardData 관련 파일들 백업
for file in "${CARDDATA_FILES[@]}"; do
    if [ -f "$file" ]; then
        cp "$file" "$BACKUP_DIR/"
        log_info "백업됨: $file"
    else
        log_warning "파일을 찾을 수 없음: $file"
    fi
done

# 5. 백업 메타데이터 생성
log_info "백업 메타데이터 생성 중..."
cat > "$BACKUP_DIR/backup_info.txt" << EOF
CardData 리팩토링 백업 정보
==========================

백업 생성 시간: $(date)
백업 생성자: $(whoami)
Git 커밋 해시: $(git rev-parse HEAD)
Git 브랜치: $CURRENT_BRANCH
Git 상태:
$(git status --porcelain)

백업된 파일 목록:
$(find "$BACKUP_DIR" -type f | grep -v backup_info.txt | sort)

백업 목적: CardData 리팩토링 전 안전 백업
리팩토링 계획서: CardData_Refactoring_Plan_v2.md
EOF

log_success "백업 완료: $BACKUP_DIR"

# 6. 현재 변경사항 스테이징 (자동)
if git status --porcelain | grep -q .; then
    log_warning "현재 작업 디렉토리에 변경사항이 있습니다."
    log_info "변경사항을 스테이징하고 커밋 중..."
    git add .
    git commit -m "feat: 리팩토링 전 현재 상태 커밋

🔄 CardData 리팩토링 준비를 위한 현재 상태 저장
- 백업 디렉토리: $BACKUP_DIR
- 리팩토링 계획서: CardData_Refactoring_Plan_v2.md

🤖 Generated with Claude Code

Co-Authored-By: Claude <noreply@anthropic.com>"
    log_success "변경사항이 커밋되었습니다."
fi

# 7. feature/carddata-refactoring 브랜치 생성
FEATURE_BRANCH="feature/carddata-refactoring"
log_info "Feature 브랜치 생성 중: $FEATURE_BRANCH"

if git show-ref --verify --quiet refs/heads/$FEATURE_BRANCH; then
    log_warning "브랜치 '$FEATURE_BRANCH'가 이미 존재합니다."
    log_info "기존 브랜치를 체크아웃합니다."
    git checkout "$FEATURE_BRANCH"
    log_success "브랜치 '$FEATURE_BRANCH'로 전환되었습니다."
    exit 0
fi

# 새 브랜치 생성 및 체크아웃
git checkout -b "$FEATURE_BRANCH"
log_success "새 브랜치 '$FEATURE_BRANCH'가 생성되고 체크아웃되었습니다."

# 8. 브랜치 설정 완료 메시지
echo
echo "============================================"
log_success "Phase 1.1 완료: 백업 및 브랜치 생성"
echo "============================================"
echo
log_info "✅ 백업 위치: $BACKUP_DIR"
log_info "✅ 현재 브랜치: $(git branch --show-current)"
log_info "✅ 다음 단계: Phase 1.2 - 아키텍처 설계"
echo
log_warning "주의: 백업 디렉토리를 안전한 곳에 보관하세요."
log_warning "리팩토링 중 문제 발생 시 백업을 사용하여 복원할 수 있습니다."
echo

# 9. 다음 단계 안내
echo "다음 실행할 명령어:"
echo "  git status                    # 현재 상태 확인"
echo "  git log --oneline -5          # 최근 커밋 확인"
echo "  ls -la $BACKUP_DIR            # 백업 파일 확인"
echo