
using cursooV1;
using Swed64;
using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Vortice.Direct3D11;

Console.WriteLine("Programmed by Kürşat Sinan");

Swed swed = new Swed("cs2");
IntPtr client = swed.GetModuleBase("client.dll");

Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();

Vector2 screenSize = renderer.screenSize;

Entity localPlayer = new Entity();
List<Entity> entities = new List<Entity>();

const int AIMBOT_HOTKEY = 0x10;  // SHIFT
const int SPACE_BAR = 0x20;      // BHOP
const int INSERT = 0x2D;         // CLOSE CHEAT
const int TRIGGER_HOTKEY = 0x14; // CAPS LOCK
const uint STANDING = 65665;
const uint CROUCHING = 65667;
const uint PLUS_JUMP = 65537;
const uint MINUS_JUMP = 256;
IntPtr forceJumpAdress = client + 0x181C670;


while (true)
{
    entities.Clear();
    IntPtr entityList = swed.ReadPointer(client, Offsets.dwEntityList);
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
    IntPtr localPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
    localPlayer.team = swed.ReadInt(localPlayerPawn, Offsets.m_iTeamNum);
    localPlayer.pawnAdress = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);
    localPlayer.origin = swed.ReadVec(localPlayer.pawnAdress, Offsets.m_vOldOrigin);
    localPlayer.view = swed.ReadVec(localPlayer.pawnAdress, Offsets.m_vecViewOffset);
    uint fFlag = swed.ReadUInt(localPlayerPawn, 0x3CC);
    int entIndex = swed.ReadInt(localPlayerPawn, Offsets.m_iIDEntIndex);

    IntPtr cameraServices = swed.ReadPointer(localPlayerPawn, Offsets.m_pCameraServices);
    uint playerFov = (uint)renderer.playerFov;
    uint currentFov = swed.ReadUInt(cameraServices + Offsets.m_iFOV);
    bool isScoped = swed.ReadBool(localPlayerPawn, Offsets.m_bIsScoped);

    if (GetAsyncKeyState(INSERT) < 0) //Close App
    {
        swed.WriteUInt(cameraServices + Offsets.m_iFOV, 90);
        Thread.Sleep(250);
        Environment.Exit(0);
    }

    if(!isScoped && currentFov != playerFov) // FOV CHANGE METHOD
    {
        swed.WriteUInt(cameraServices + Offsets.m_iFOV, playerFov);
    }


    if (renderer.enableBHOP && GetAsyncKeyState(SPACE_BAR) < 0)// BHOP METHOD
    {
        if (fFlag == STANDING || fFlag == CROUCHING)
        {
            Thread.Sleep(1);
            swed.WriteUInt(forceJumpAdress, PLUS_JUMP);
        }
        else
        {
            Thread.Sleep(5);
            swed.WriteUInt(forceJumpAdress, MINUS_JUMP);
        }
    }

    if (renderer.enableFlashBlock) // FLASH BLOCK METHOD
    {
        float flashDuration = swed.ReadFloat(localPlayerPawn, Offsets.m_flFlashBangTime);

        if (flashDuration > 0)
        {
            swed.WriteFloat(localPlayerPawn, Offsets.m_flFlashBangTime, 0);
            Console.WriteLine("Flash removed");
        }
    }else continue;
    
    for (int i = 0; i < 64; i++) // ENTITY LIST LOOP
    {
        if (listEntry == IntPtr.Zero) // CHECKING ENTITY IS VALID OR NOT.
            continue;

        IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);
        if (currentController == IntPtr.Zero) continue;

        int pawnHandle = swed.ReadInt(currentController, Offsets.m_hPlayerPawn);
        if (pawnHandle == 0) continue;

        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        if (listEntry2 == IntPtr.Zero) continue;

        IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
        if (currentPawn == localPlayer.pawnAdress) continue;

        uint lifeState = swed.ReadUInt(currentPawn, Offsets.m_lifeState); // IS ENTITY ALIVE ?
        if (lifeState != 256) continue;

        int health = swed.ReadInt(currentPawn, Offsets.m_iHealth);
        int team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        // bool spotted = swed.ReadBool(currentPawn, Offsets.m_entitySpottedState + Offsets.m_bSpotted);         Working on it.
        // if (spotted == false && renderer.aimOnlySpotted) continue;                                            Working on it.

        if (team == localPlayer.team && !renderer.aimOnTeam)
            continue;

        float[] viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);

        IntPtr sceneNode = swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, Offsets.m_modelState + 0x80);

        Entity entity = new Entity();

        entity.team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);                                                    // Wallhack values
        entity.pos = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);                                                   // Wallhack values
        entity.viewOffset = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);                                         // Wallhack values
        entity.pos2D = Calculate.WorldtoScreen(viewMatrix, entity.pos, screenSize);                                     // Wallhack values
        entity.viewPos2D = Calculate.WorldtoScreen(viewMatrix, Vector3.Add(entity.pos, entity.viewOffset), screenSize); // Wallhack values
        entity.bones = Calculate.ReadBones(boneMatrix,swed);
        entity.bones2D = Calculate.ReadBones2d(entity.bones, viewMatrix, screenSize);
        entity.pawnAdress = currentPawn;                                                                                // AIMBOT VALUES
        entity.controllerAdress = currentController;                                                                    // AIMBOT VALUES
        entity.health = health;                                                                                         // AIMBOT VALUES
        entity.lifeState = lifeState;                                                                                   // AIMBOT VALUES
        entity.origin = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);                                                // AIMBOT VALUES
        entity.view = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);                                               // AIMBOT VALUES
        entity.distance = Vector3.Distance(entity.origin, localPlayer.origin);                                          // AIMBOT VALUES - PLAYER
        entity.head = swed.ReadVec(boneMatrix, 6 * 32);                                                                 // AIMBOT VALUES
        entity.head2d = Calculate.WorldtoScreen(viewMatrix, entity.head, screenSize);                                   // AIMBOT VALUES
        entity.pixelDistance = Vector2.Distance(entity.head2d, new Vector2(screenSize.X / 2, screenSize.Y / 2));        // AIMBOT VALUES - CROSSHAIR

        entities.Add(entity);
    }
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);

    entities = entities.OrderBy(o => o.pixelDistance).ToList(); // ORDERING ENTITIES BY DISTANCE FROM CROSSHAIR

    if (renderer.enableAimbot && entities.Count > 0 && GetAsyncKeyState(AIMBOT_HOTKEY) < 0) // AIMBOT METHOD
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

    if(entIndex != -1 && GetAsyncKeyState(TRIGGER_HOTKEY) < 0 && renderer.enableTrigger) //TRIGGER METHOD
    {
        swed.WriteInt(client,Offsets.dwForceAttack, 65537);
        Thread.Sleep(2);
        swed.WriteInt(client,Offsets.dwForceAttack, 256);
        Thread.Sleep(2); // FOR SLOW ATTACK YOU CAN CHANGE THIS VALUE(IT WILL LAGGY WALLHACK)
    }
}

[DllImport("user32.dll")] //TRACKING KEYBOARD INPUTS
static extern short GetAsyncKeyState(int vKey);