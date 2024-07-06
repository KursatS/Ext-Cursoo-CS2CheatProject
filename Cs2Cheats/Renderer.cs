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
        public bool enableBHOP = false;
        public bool enableTrigger = true;
        public bool aimOnTeam = false;
        public bool aimOnlySpotted = true;
        public bool enableESPLines = true;
        public bool enableBonesEsp = true;
        public bool enableNoRecoil = false;
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
            ImGui.Checkbox("Enable No Recoil (BETA)", ref enableNoRecoil);
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
            SetupImGuiStyle();
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

        public static void SetupImGuiStyle()
        {
            // Purple Comfy styleRegularLunar from ImThemes
            var style = ImGuiNET.ImGui.GetStyle();

            style.Alpha = 1.0f;
            style.DisabledAlpha = 0.1000000014901161f;
            style.WindowPadding = new Vector2(8.0f, 8.0f);
            style.WindowRounding = 10.0f;
            style.WindowBorderSize = 0.0f;
            style.WindowMinSize = new Vector2(30.0f, 30.0f);
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Right;
            style.ChildRounding = 5.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 10.0f;
            style.PopupBorderSize = 0.0f;
            style.FramePadding = new Vector2(5.0f, 3.5f);
            style.FrameRounding = 5.0f;
            style.FrameBorderSize = 0.0f;
            style.ItemSpacing = new Vector2(5.0f, 4.0f);
            style.ItemInnerSpacing = new Vector2(5.0f, 5.0f);
            style.CellPadding = new Vector2(4.0f, 2.0f);
            style.IndentSpacing = 5.0f;
            style.ColumnsMinSpacing = 5.0f;
            style.ScrollbarSize = 15.0f;
            style.ScrollbarRounding = 9.0f;
            style.GrabMinSize = 15.0f;
            style.GrabRounding = 5.0f;
            style.TabRounding = 5.0f;
            style.TabBorderSize = 0.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

            style.Colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(1.0f, 1.0f, 1.0f, 0.3605149984359741f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.3803921639919281f, 0.4235294163227081f, 0.572549045085907f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.09803921729326248f, 0.09803921729326248f, 0.09803921729326248f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.2588235437870026f, 0.2588235437870026f, 0.2588235437870026f, 0.0f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 0.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.1568627506494522f, 0.1568627506494522f, 0.1568627506494522f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.2352941185235977f, 0.2352941185235977f, 0.2352941185235977f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.294117659330368f, 0.294117659330368f, 0.294117659330368f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.0f, 0.4509803950786591f, 1.0f, 0.0f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.1333333402872086f, 0.2588235437870026f, 0.4235294163227081f, 0.0f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.294117659330368f, 0.294117659330368f, 0.294117659330368f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.7372549176216125f, 0.6941176652908325f, 0.886274516582489f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.2901960909366608f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 0.03433477878570557f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.501960813999176f, 0.3019607961177826f, 1.0f, 0.5490196347236633f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 0.8999999761581421f);
            style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 0.699999988079071f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.2000000029802322f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.3499999940395355f);
        }

    }
}
