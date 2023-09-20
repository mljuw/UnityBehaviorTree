namespace Pandora.BehaviorTree
{
    public interface IDebugableBTElement
    {
        public void SetActivation(bool activated);

        public void DebugTick(float deltaTime);
    }
}