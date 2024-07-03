using ClickableTransparentOverlay;
using ImGuiNET;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Cs2Cheats
{
    public class Renderer : Overlay
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vkey);

        public Vector2 screenSize = new Vector2(1920, 1080);

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        ImDrawListPtr drawList;

        public bool showMenu = true;
        public bool enableESP = true;
        public bool enableFlashBlock = true;
        public bool enableAimbot = true;
        public bool enableBHOP = true;
        public bool enableTrigger = true;
        public bool aimOnTeam = false;
        public float circleFov = 50;
        public int playerFov = 90;
        public Vector4 circleColor = new Vector4(1,1,1,1);    // WHITE
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1); // RED
        private Vector4 teamColor = new Vector4(0, 1, 0, 1);  // GREEN

        protected override void Render()
        {
            ImGui.Begin("by Cursoo^^");
            ImGui.Checkbox("Enable ESP", ref enableESP);
            ImGui.Checkbox("Enable Trigger", ref enableTrigger);
            ImGui.Checkbox("Enable Aimbot", ref enableAimbot);
            ImGui.SameLine();
            ImGui.Checkbox("Enable Aim on Team", ref aimOnTeam);
            ImGui.SliderFloat("Aimbot FOV", ref circleFov, 10, 300);
            ImGui.SliderInt("Player FOV", ref playerFov, 85, 160);
            ImGui.Checkbox("Enable Flash Block", ref enableFlashBlock);
            ImGui.Checkbox("Enable BHOP", ref enableBHOP);

            if (ImGui.CollapsingHeader("FOV Circle Color"))
                ImGui.ColorPicker4("##circlecolor",ref circleColor);

            if (ImGui.CollapsingHeader("Enemy color"))
                ImGui.ColorPicker4("##enemycolor", ref enemyColor);

            if (ImGui.CollapsingHeader("Team color"))
                ImGui.ColorPicker4("##teamcolor", ref teamColor);

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();
            drawList.AddCircle(new Vector2(screenSize.X / 2, screenSize.Y / 2), circleFov, ImGui.ColorConvertFloat4ToU32(circleColor));

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    //ImGui.Text($"Entity Screen Position: {entity.pos2D.X}, {entity.pos2D.Y}"); // For seeing all entities position at the top left on screen
                    if (EntityOnScreen(entity))
                    {
                        DrawHealthBar(entity);
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

        private void DrawHealthBar(Entity entity)
        {
            float entityHeight = entity.pos2D.Y - entity.viewPos2D.Y;
            float boxLeft = entity.viewPos2D.X - entityHeight / 3;
            float boxRight = entity.pos2D.X + entityHeight / 3;
            float barWidth = 0.05f;
            float barHeight = entityHeight * (entity.health / 100f);
            float barPixelWidth = barWidth * (boxRight - boxLeft);

            Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.pos2D.Y - barHeight);
            Vector2 barBottom = new Vector2(boxLeft, entity.pos2D.Y);
            Vector4 barColor = new Vector4(0, 1, 0, 1);

            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));
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
