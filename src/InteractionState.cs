namespace CityBuilderPOV
{
    // Kept independent of Unity so input transitions can be tested without the game.
    public sealed class InteractionState
    {
        public bool MenuOpen { get; private set; }
        public bool ToolActive { get; private set; }
        public bool WaitingForRelease { get; private set; }
        private int transitionFrame;
        public bool BlocksActions => MenuOpen || ToolActive || WaitingForRelease;
        public bool CanPlace => ToolActive && !MenuOpen && !WaitingForRelease;
        public void OpenMenu(int frame) { MenuOpen = true; ToolActive = false; Fence(frame); }
        public void CloseMenu(int frame) { MenuOpen = false; Fence(frame); }
        public void StartTool(int frame) { MenuOpen = false; ToolActive = true; Fence(frame); }
        public void StopTool(int frame) { ToolActive = false; Fence(frame); }
        public void Fence(int frame) { WaitingForRelease = true; transitionFrame = frame; }
        public void Release(int frame, bool leftHeld, bool rightHeld)
        {
            if (frame > transitionFrame && !leftHeld && !rightHeld) WaitingForRelease = false;
        }
        public void Reset() { MenuOpen = false; ToolActive = false; WaitingForRelease = false; }
        public static float Rotate(float yaw, int direction, float step)
        {
            if (float.IsNaN(yaw) || float.IsInfinity(yaw)) yaw = 0;
            if (float.IsNaN(step) || float.IsInfinity(step) || step <= 0) return yaw;
            return ((yaw + direction * step) % 360 + 360) % 360;
        }
        public static bool WithinReach(float squaredDistance, float reach)
        {
            return !float.IsNaN(squaredDistance) && !float.IsInfinity(squaredDistance)
                && !float.IsNaN(reach) && !float.IsInfinity(reach)
                && reach > 0 && squaredDistance >= 0 && squaredDistance <= reach * reach;
        }
    }
}
