using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 그리드 서비스 구현체 - 의존성 주입을 위한 서비스 컨테이너
    /// </summary>
    public class GridServices : IGridServices
    {
        public IGridState GridState { get; }
        public IGridController GridController { get; }
        public IGridRenderer GridRenderer { get; }
        
        public GridServices(IGridState state, IGridController controller, IGridRenderer renderer)
        {
            GridState = state ?? throw new System.ArgumentNullException(nameof(state));
            GridController = controller ?? throw new System.ArgumentNullException(nameof(controller));
            GridRenderer = renderer ?? throw new System.ArgumentNullException(nameof(renderer));
        }

        public override string ToString()
        {
            return $"GridServices[State:{GridState?.GetType().Name}, Controller:{GridController?.GetType().Name}, Renderer:{GridRenderer?.GetType().Name}]";
        }
    }
}