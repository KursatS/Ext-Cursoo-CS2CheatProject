using ClickableTransparentOverlay;
using ImGuiNET;
using System.Collections.Concurrent;
using System.Numerics;

namespace Cs2Cheats
{
    public class Renderer : Overlay
    {
        public Vector2 screenSize = new Vector2(1920, 1080);

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        ImDrawListPtr drawList;

        private bool enableESP = true;
        public bool enableFlashBlock = true;
        public bool enableAimbot = true;
        public bool enableBHOP = true;
        public bool aimOnTeam = false;
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1); //RED
        private Vector4 teamColor = new Vector4(0, 1, 0, 1); //GREEN

        protected override void Render()
        {
            ImGui.Begin("by Cursoo^^");
            ImGui.Checkbox("Enable ESP", ref enableESP);
            ImGui.Checkbox("Enable Aimbot", ref enableAimbot);
            ImGui.Checkbox("Enable Flash Block", ref enableFlashBlock);
            ImGui.Checkbox("Enable BHOP", ref enableBHOP);

            if (ImGui.CollapsingHeader("Team color"))
                ImGui.ColorPicker4("##teamcolor", ref teamColor);
            if (ImGui.CollapsingHeader("Enemy color"))
                ImGui.ColorPicker4("##enemycolor", ref enemyColor);

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    //ImGui.Text($"Entity Screen Position: {entity.pos2D.X}, {entity.pos2D.Y}"); // For seeing all entities position at the top left on screen
                    if (EntityOnScreen(entity))
                    {
                        DrawBox(entity);
                        DrawLine(entity);
                    }
                }
            }
        }
        bool EntityOnScreen(Entity entity)
        {
            if (entity.pos2D.X > 0 && entity.pos2D.X < screenSize.X && entity.pos2D.Y > 0 && entity.pos2D.Y < screenSize.Y)
            {
                return true;
            }
            return false;
        }
        private void DrawBox(Entity entity)
        {
            float entityHeight = entity.pos2D.Y - entity.viewPos2D.Y;
            Vector2 rectTop = new Vector2(entity.viewPos2D.X - entityHeight / 3, entity.viewPos2D.Y);
            Vector2 rectBottom = new Vector2(entity.pos2D.X + entityHeight / 3, entity.pos2D.Y);
            Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;

            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }
        private void DrawLine(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.pos2D, ImGui.ColorConvertFloat4ToU32(lineColor));
        }
        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }
        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }
        void DrawOverlay(Vector2 screenSize) //Cheat Menu Overlay
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse);
        }
    }
}
