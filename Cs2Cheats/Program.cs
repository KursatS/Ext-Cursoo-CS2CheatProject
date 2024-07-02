
using Cs2Cheats;
using Swed64;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

Console.WriteLine("Programmed by Kürşat Sinan");

Swed swed = new Swed("cs2");
IntPtr client = swed.GetModuleBase("client.dll");

Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();

Vector2 screenSize = renderer.screenSize;
Console.WriteLine($"Screen Size: {screenSize.X}x{screenSize.Y}");

Entity localPlayer = new Entity();
List<Entity> entities = new List<Entity>();

const int AIMBOT_HOTKEY = 0x10; // SHIFT
const int SPACE_BAR = 0x20;
const int TRIGGER_HOTKEY = 0x05; // MOUSE 4 OR MOUSE 5
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

    if(entIndex != -1 && (GetAsyncKeyState(TRIGGER_HOTKEY)) < 0 && renderer.enableTrigger)
    {
        swed.WriteInt(client,Offsets.dwForceAttack, 65537);
        Thread.Sleep(10);
        swed.WriteInt(client,Offsets.dwForceAttack, 256);
        Thread.Sleep(2);
    }

    if (renderer.enableBHOP && GetAsyncKeyState(SPACE_BAR) < 0)
    {
        if (fFlag == STANDING || fFlag == CROUCHING)
        {
            Thread.Sleep(1);
            swed.WriteUInt(forceJumpAdress, PLUS_JUMP);
        }
        else
        {
            swed.WriteUInt(forceJumpAdress, MINUS_JUMP);
        }
        Thread.Sleep(5);
    }

    if (renderer.enableFlashBlock)
    {
        float flashDuration = swed.ReadFloat(localPlayerPawn, Offsets.m_flFlashBangTime);

        if (flashDuration > 0)
        {
            swed.WriteFloat(localPlayerPawn, Offsets.m_flFlashBangTime, 0);
            Console.WriteLine("Flash removed");
        }
    }
    else
    {
        continue;
    }

    for (int i = 0; i < 64; i++)
    {
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

        IntPtr sceneNode = swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode);

        IntPtr boneMatrix = swed.ReadPointer(sceneNode, Offsets.m_modelState + 0x80);

        int health = swed.ReadInt(currentPawn, Offsets.m_iHealth);
        int team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        if (team == localPlayer.team && !renderer.aimOnTeam)
            continue;


        float[] viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);

        Entity entity = new Entity();

        entity.pawnAdress = currentPawn;
        entity.controllerAdress = currentController;
        entity.health = health;
        entity.lifeState = lifeState;
        entity.origin = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);
        entity.team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        entity.pos = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);
        entity.viewOffset = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);
        entity.view = swed.ReadVec(currentPawn, Offsets.m_vecViewOffset);
        entity.distance = Vector3.Distance(entity.origin, localPlayer.origin);
        entity.pos2D = Calculate.WorldtoScreen(viewMatrix, entity.pos, screenSize);
        entity.viewPos2D = Calculate.WorldtoScreen(viewMatrix, Vector3.Add(entity.pos, entity.viewOffset), screenSize);
        entity.head = swed.ReadVec(boneMatrix, 6 * 32);

        entities.Add(entity);
    }
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);

    entities = entities.OrderBy(o => o.distance).ToList();

    if (renderer.enableAimbot && entities.Count > 0 && GetAsyncKeyState(AIMBOT_HOTKEY) < 0)
    {
        Vector3 playerView = Vector3.Add(localPlayer.origin, localPlayer.view);
        Vector3 entityView = Vector3.Add(entities[0].origin, entities[0].view);
        Vector2 newAngles = Calculate.CalculateAngles(playerView, entities[0].head);
        Vector3 newAnglesVec3 = new Vector3(newAngles.Y, newAngles.X, 0.0f);

        swed.WriteVec(client, Offsets.dwViewAngles, newAnglesVec3);
    }

}

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey);