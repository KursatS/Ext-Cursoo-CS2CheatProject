using ClickableTransparentOverlay;
using ImGuiNET;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace cursooV1
{
    public class Renderer : Overlay
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vkey);

        static int screenW = Screen.PrimaryScreen.Bounds.Width;
        static int screenH = Screen.PrimaryScreen.Bounds.Height;

        public Vector2 screenSize = new Vector2(screenW, screenH);

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        ImDrawListPtr drawList;

        public bool enableESP = true;
        public bool enableFlashBlock = true;
        public bool enableAimbot = true;
        public bool enableBHOP = true;
        public bool enableTrigger = true;
        public bool aimOnTeam = false;
        public bool aimOnlySpotted = true;
        public bool enableESPLines = true;
        public bool enableBonesEsp = true;
        public float circleFov = 50;
        public float boneThickness = 400;
        public int playerFov = 90;
        public Vector4 circleColor = new Vector4(1,1,1,1);    // WHITE
        public Vector4 enemyColor = new Vector4(1, 0, 0, 1); // RED
        public Vector4 teamColor = new Vector4(0, 1, 0, 1);  // GREEN


        static void HelpMarker(string text)
        {
            ImGui.TextDisabled("?");
            if (ImGui.BeginItemTooltip())
            {
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        protected override void Render()
        {
            ImGui.Begin("by Cursoo^^");
            ImGui.Text("INSERT for exit");
            ImGui.Checkbox("Enable ESP", ref enableESP);
            ImGui.SameLine();
            ImGui.Checkbox("Enable ESP Lines", ref enableESPLines);
            ImGui.Checkbox("Enable Bones ESP", ref enableBonesEsp);
            ImGui.Checkbox("Enable Trigger", ref enableTrigger);
            ImGui.SameLine(); HelpMarker("HOLD to CAPS LOCK");
            ImGui.Checkbox("Enable Aimbot", ref enableAimbot);
            ImGui.SameLine(); HelpMarker("HOLD to SHIFT");
            //ImGui.Checkbox("Enable Aim Only Spotted", ref aimOnlySpotted); Working on it.
            ImGui.Checkbox("Enable Aim on Team", ref aimOnTeam);
            ImGui.SliderFloat("Aimbot FOV", ref circleFov, 10, 300);
            ImGui.SliderInt("Player FOV", ref playerFov, 85, 160);
            ImGui.Checkbox("Enable Flash Block", ref enableFlashBlock);
            ImGui.Checkbox("Enable BHOP", ref enableBHOP);

            if (ImGui.TreeNode("Color Settings"))
            {
                if (ImGui.CollapsingHeader("FOV Circle Color"))
                    ImGui.ColorPicker4("##circlecolor", ref circleColor);

                if (ImGui.CollapsingHeader("Enemy color"))
                    ImGui.ColorPicker4("##enemycolor", ref enemyColor);

                if (ImGui.CollapsingHeader("Team color"))
                    ImGui.ColorPicker4("##teamcolor", ref teamColor);
                ImGui.TreePop();
            }
            ImGui.Separator();
            if (ImGui.Button("GITHUB"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/KursatS/Cs2-External-Cheat-Project",
                    UseShellExecute = true
                });
            }

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();
            drawList.AddCircle(new Vector2(screenSize.X / 2, screenSize.Y / 2), circleFov, ImGui.ColorConvertFloat4ToU32(circleColor));

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        DrawBox(entity);
                        if (entity.health >= 70)
                        {
                            Vector4 healthBarColor = new Vector4(0, 1, 0, 1);
                            DrawHealthBar(entity, healthBarColor);
                        }
                        else if (entity.health >= 30)
                        {
                            Vector4 healthBarColor = new Vector4(1, 1, 0, 1);
                            DrawHealthBar(entity, healthBarColor);
                        }
                        else
                        {
                            Vector4 healthBarColor = new Vector4(1, 0, 0, 1);
                            DrawHealthBar(entity, healthBarColor);
                        }
                    }
                }
            }
            if (enableESPLines)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        DrawLine(entity);
                    }
                }
            }
            if (enableBonesEsp)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        if (entity.health >= 70)
                        {
                            Vector4 boneColor = new Vector4(0, 1, 0, 1);
                            DrawBone(entity, boneColor);
                        }else if (entity.health >= 30)
                        {
                            Vector4 boneColor = new Vector4(1, 1, 0, 1);
                            DrawBone(entity, boneColor);
                        }
                        else
                        {
                            Vector4 boneColor = new Vector4(1, 0, 0, 1);
                            DrawBone(entity, boneColor);
                        }
                            
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

        private void DrawBone(Entity entity,Vector4 boneColor)
        {
            uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

            float currentBoneThickness = boneThickness / entity.distance;

            drawList.AddLine(entity.bones2D[1], entity.bones2D[2], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[1], entity.bones2D[3], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[1], entity.bones2D[6], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[3], entity.bones2D[4], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[6], entity.bones2D[7], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[4], entity.bones2D[5], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[7], entity.bones2D[8], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[1], entity.bones2D[0], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[0], entity.bones2D[9], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[0], entity.bones2D[11], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[9], entity.bones2D[10], uintColor, currentBoneThickness);
            drawList.AddLine(entity.bones2D[11], entity.bones2D[12], uintColor, currentBoneThickness);
            drawList.AddCircle(entity.bones2D[2], 3 + currentBoneThickness, uintColor);
        }

        private void DrawHealthBar(Entity entity, Vector4 healthBarColor)
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

            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(healthBarColor));
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
