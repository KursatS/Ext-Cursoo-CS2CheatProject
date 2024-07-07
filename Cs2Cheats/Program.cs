using Swed64;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D11;

namespace cursooV1
{
    class Program
    {
        private static readonly Swed swed = new Swed("cs2");
        private static readonly IntPtr client = swed.GetModuleBase("client.dll");
        private static readonly Renderer renderer = new Renderer();
        private static readonly Vector2 screenSize = renderer.screenSize;
        private static readonly List<Entity> entities = new List<Entity>();
        private static readonly Entity localPlayer = new Entity();

        private const int AIMBOT_HOTKEY = 0x10;  // SHIFT
        private const int SPACE_BAR = 0x20;      // BHOP
        private const int INSERT = 0x2D;         // CLOSE CHEAT
        private const int TRIGGER_HOTKEY = 0x14; // CAPS LOCK
        private const uint STANDING = 65665;
        private const uint CROUCHING = 65667;
        private const uint PLUS_JUMP = 65537;
        private const uint MINUS_JUMP = 256;
        private static readonly IntPtr forceJumpAddress = client + 0x181C670;
        private static Vector3 oldPunch = new Vector3();

        static void Main()
        {
            Thread renderThread = new Thread(() => renderer.Start().Wait());
            renderThread.Start();

            while (true)
            {
                UpdateEntities();
                HandleNoRecoil();
                HandleFovChange();
                HandleBhop();
                HandleFlashBlock();
                HandleAimbot();
                HandleTrigger();

                if (GetAsyncKeyState(INSERT) < 0) // Close App
                {
                    CloseApplication();
                }
            }
        }

        private static void UpdateEntities()
        {
            entities.Clear();
            IntPtr entityList = swed.ReadPointer(client, Offsets.dwEntityList);
            IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            localPlayer.team = swed.ReadInt(localPlayerPawn, Offsets.m_iTeamNum);
            localPlayer.pawnAdress = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            localPlayer.origin = swed.ReadVec(localPlayer.pawnAdress, Offsets.m_vOldOrigin);
            localPlayer.view = swed.ReadVec(localPlayer.pawnAdress, Offsets.m_vecViewOffset);
            localPlayer.crosshairCoordinates = swed.ReadVec(client + Offsets.dwViewAngles);

            for (int i = 0; i < 64; i++)
            {
                if (listEntry == IntPtr.Zero)
                    continue;

                IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);
                if (currentController == IntPtr.Zero) continue;

                int pawnHandle = swed.ReadInt(currentController, Offsets.m_hPlayerPawn);
                if (pawnHandle == 0) continue;

                IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                if (listEntry2 == IntPtr.Zero) continue;

                IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
                if (currentPawn == localPlayer.pawnAdress) continue;

                uint lifeState = swed.ReadUInt(currentPawn, Offsets.m_lifeState);
                if (lifeState != 256) continue;

                Entity entity = new Entity
                {
                    team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum),
                    pos = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin),
                    name = swed.ReadString(currentController, Offsets.m_iszPlayerName, 16),
                    spotted = swed.ReadBool(currentPawn, Offsets.m_entitySpottedState + Offsets.m_bSpotted),
                    viewOffset = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset),
                    pos2D = Calculate.WorldtoScreen(swed.ReadMatrix(client + Offsets.dwViewMatrix), swed.ReadVec(currentPawn, Offsets.m_vOldOrigin), screenSize),
                    viewPos2D = Calculate.WorldtoScreen(swed.ReadMatrix(client + Offsets.dwViewMatrix), Vector3.Add(swed.ReadVec(currentPawn, Offsets.m_vOldOrigin), swed.ReadVec(currentPawn, Offsets.m_vecViewOffset)), screenSize),
                    bones = Calculate.ReadBones(swed.ReadPointer(swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode), Offsets.m_modelState + 0x80), swed),
                    bones2D = Calculate.ReadBones2d(Calculate.ReadBones(swed.ReadPointer(swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode), Offsets.m_modelState + 0x80), swed), swed.ReadMatrix(client + Offsets.dwViewMatrix), screenSize),
                    pawnAdress = currentPawn,
                    controllerAdress = currentController,
                    health = swed.ReadInt(currentPawn, Offsets.m_iHealth),
                    lifeState = lifeState,
                    origin = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin),
                    view = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset),
                    distance = Vector3.Distance(swed.ReadVec(currentPawn, Offsets.m_vOldOrigin), localPlayer.origin),
                    head = swed.ReadVec(swed.ReadPointer(swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode), Offsets.m_modelState + 0x80), 6 * 32),
                    head2d = Calculate.WorldtoScreen(swed.ReadMatrix(client + Offsets.dwViewMatrix), swed.ReadVec(swed.ReadPointer(swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode), Offsets.m_modelState + 0x80), 6 * 32), screenSize),
                    pixelDistance = Vector2.Distance(Calculate.WorldtoScreen(swed.ReadMatrix(client + Offsets.dwViewMatrix), swed.ReadVec(swed.ReadPointer(swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode), Offsets.m_modelState + 0x80), 6 * 32), screenSize), new Vector2(screenSize.X / 2, screenSize.Y / 2))
                };

                if (entity.team == localPlayer.team && !renderer.aimOnTeam)
                    continue;

                entities.Add(entity);

                if(renderer.enableRadarHack)
                    swed.WriteBool(currentPawn, Offsets.m_entitySpottedState + Offsets.m_bSpotted, true);
                else
                    swed.WriteBool(currentPawn, Offsets.m_entitySpottedState + Offsets.m_bSpotted, false);
            }

            renderer.UpdateLocalPlayer(localPlayer);
            renderer.UpdateEntities(entities);
        }

        private static void HandleNoRecoil()
        {
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            bool shotsFired = swed.ReadBool(localPlayerPawn, Offsets.m_iShotsFired);
            Vector3 aimPunch = swed.ReadVec(localPlayerPawn, Offsets.m_aimPunchAngle);

            if (shotsFired && renderer.enableNoRecoil)
            {
                Vector3 newAngles = new Vector3(
                    localPlayer.crosshairCoordinates.X + oldPunch.X - aimPunch.X * 2,
                    localPlayer.crosshairCoordinates.Y + oldPunch.Y - aimPunch.Y * 2,
                    0);

                swed.WriteVec(client + Offsets.dwViewAngles, newAngles);

                oldPunch.X = aimPunch.X * 2;
                oldPunch.Y = aimPunch.Y * 2;
            }
            else
            {
                oldPunch.X = oldPunch.Y = 0;
            }
        }

        private static void HandleFovChange()
        {
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            IntPtr cameraServices = swed.ReadPointer(localPlayerPawn, Offsets.m_pCameraServices);
            uint playerFov = (uint)renderer.playerFov;
            uint currentFov = swed.ReadUInt(cameraServices + Offsets.m_iFOV);
            bool isScoped = swed.ReadBool(localPlayerPawn, Offsets.m_bIsScoped);

            if (!isScoped && currentFov != playerFov)
            {
                swed.WriteUInt(cameraServices + Offsets.m_iFOV, playerFov);
            }
        }

        private static void HandleBhop()
        {
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            uint fFlag = swed.ReadUInt(localPlayerPawn, 0x3CC);

            if (renderer.enableBHOP && GetAsyncKeyState(SPACE_BAR) < 0)
            {
                if (fFlag == STANDING || fFlag == CROUCHING)
                {
                    Thread.Sleep(1);
                    swed.WriteUInt(forceJumpAddress, PLUS_JUMP);
                }
                else
                {
                    Thread.Sleep(1);
                    swed.WriteUInt(forceJumpAddress, MINUS_JUMP);
                }
            }
        }

        private static void HandleFlashBlock()
        {
            if (renderer.enableFlashBlock)
            {
                IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
                float flashDuration = swed.ReadFloat(localPlayerPawn, Offsets.m_flFlashBangTime);

                if (flashDuration > 0)
                {
                    swed.WriteFloat(localPlayerPawn, Offsets.m_flFlashBangTime, 0);
                }
            }
        }

        private static void HandleAimbot()
        {
            entities.Sort((a, b) => a.pixelDistance.CompareTo(b.pixelDistance));

            if (renderer.enableAimbot && entities.Count > 0 && GetAsyncKeyState(AIMBOT_HOTKEY) < 0)
            {
                Vector3 playerView = Vector3.Add(localPlayer.origin, localPlayer.view);
                Vector3 entityView = Vector3.Add(entities[0].origin, entities[0].view);

                if (entities[0].pixelDistance < renderer.circleFov)
                {
                    Vector2 newAngles = Calculate.CalculateAngles(playerView, entities[0].head);
                    Vector3 newAnglesVec3 = new Vector3(newAngles.Y, newAngles.X, 0.0f);

                    swed.WriteVec(client, Offsets.dwViewAngles, newAnglesVec3);
                }
            }
        }

        private static void HandleTrigger()
        {
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            int entIndex = swed.ReadInt(localPlayerPawn, Offsets.m_iIDEntIndex);

            if (entIndex != -1 && GetAsyncKeyState(TRIGGER_HOTKEY) < 0 && renderer.enableTrigger)
            {
                swed.WriteInt(client, Offsets.dwForceAttack, 65537);
                Thread.Sleep(2);
                swed.WriteInt(client, Offsets.dwForceAttack, 256);
            }
        }

        private static void CloseApplication()
        {
            IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
            IntPtr cameraServices = swed.ReadPointer(localPlayerPawn, Offsets.m_pCameraServices);
            swed.WriteUInt(cameraServices + Offsets.m_iFOV, 90);
            Thread.Sleep(250);
            Environment.Exit(0);
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
