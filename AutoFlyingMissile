const string MISSILE_GROUP_TAG = "NAIL";
const double LAUNCH_DISTANCE = 30;

/// <summary>
/// GYRO:how tight to maintain aim. Lower is tighter. Default is 0.01f
/// </summary>
const float minAngleRad = 0.01f;
/// <summary>
/// GYRO:How much power to use 0 to 1.0
/// </summary>
const double CTRL_COEFF = 0.9;

private Vector3D m_targetPosition = Vector3D.Zero;

private IMyShipMergeBlock m_missile_mergeBlock = null;
private IMyRemoteControl m_missile_remoteControl = null;
private IMyGyro m_missile_gyro = null;
//private IMyTextSurface m_missile_lcd = null;
private List<IMyThrust> m_missile_forward_thrusts = new List<IMyThrust>();
private List<IMyWarhead> m_missile_warheads = new List<IMyWarhead>();

private Vector3D m_releasePosition = Vector3D.Zero;
private bool m_missile_launch_completed = false;

public Program()
{
    // The constructor, called only once every session and
    // always before any other method is called. Use it to
    // initialize your script. 
    //     
    // The constructor is optional and can be removed if not
    // needed.
    // 
    // It's recommended to set RuntimeInfo.UpdateFrequency 
    // here, which will allow your script to run itself without a 
    // timer block.

    Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

public void Main(string argument, UpdateType updateSource)
{
    // The main entry point of the script, invoked every time
    // one of the programmable block's Run actions are invoked,
    // or the script updates itself. The updateSource argument
    // describes where the update came from.
    // 
    // The method itself is required, but the arguments above
    // can be removed if not needed.
    
    string timestamp = DateTime.Now.ToLongTimeString();
    Echo($"[{timestamp}]");
    
    // 1. try setup if not yet
    if (!HasBeenSetup())
    {
        Echo($"Setup...");
        Setup();
        return;
    }

    Echo("Setup finished. Reading target position...");

    // 2. try get target position
    // keep get updated target position...
    if(!TryGetTargetPosition(timestamp, out m_targetPosition))
    {
        return;
    }

    Echo("Load target position. Waiting for being released...");

    // 3. wait for being released
    if (!HasBeenReleased())
    {
        return;
    }
    // 4. when released, read the position
    if (Vector3D.Zero.Equals(m_releasePosition))
    {
        m_releasePosition = GetMissileCurrentPosition();
    }

    Echo("Missile released. Launching...");

    // 4. launch missile
    if (!HasCompletedLaunching())
    {
        Launch();
        return;
    }

    Echo("Missile launched. Arm warheads...");
    if (m_missile_warheads.Count() == 0)
    {
        ArmWarheads();
    }

    // 5. constinously adjust facing furing flight
    Echo("Missile launched. Aiming at target...");
    AimAtTarget();
}

#region AimingLogic

private void AimAtTarget()
{
    Vector3D missilePosition = GetMissileCurrentPosition();
    Vector3D targetDirection = Vector3D.Normalize(Vector3D.Subtract(m_targetPosition, missilePosition));

    Matrix missileOrientaion;
    m_missile_remoteControl.Orientation.GetMatrix(out missileOrientaion);

    var localCurrent = Vector3D.Transform(missileOrientaion.Forward, MatrixD.Transpose(missileOrientaion));
    var localTarget = Vector3D.Transform(targetDirection, MatrixD.Transpose(m_missile_gyro.WorldMatrix.GetOrientation()));

    var rot = Vector3D.Cross(localCurrent, localTarget);
    double dot = Vector3D.Dot(localCurrent, localTarget);
    double ang = rot.Length();
    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
    if (dot < 0)
        ang = Math.PI - ang; // compensate for >+/-90
    
    if (ang < minAngleRad)
    { // close enough 
        SetThrustPercentage(1f);
    }
    else
    {
        //DisableThrustOverride();
    }

    float yawMax = (float)(2 * Math.PI);
    double ctrl_vel = yawMax * (ang / Math.PI) * CTRL_COEFF;
    ctrl_vel = Math.Min(yawMax, ctrl_vel);
    ctrl_vel = Math.Max(0.01, ctrl_vel);
    rot.Normalize();
    rot *= ctrl_vel;

    float yaw = -(float)rot.Y;
    float pitch = -(float)rot.X;
    float roll = -(float)rot.Z;

    TurningGyro(yaw, pitch, roll);
}

private void TurningGyro(float yaw, float pitch, float roll)
{
    m_missile_gyro.Yaw = yaw;
    m_missile_gyro.Pitch = pitch;
    m_missile_gyro.Roll = roll;
    m_missile_gyro.GyroOverride = true;
}

#endregion


private bool HasBeenReleased()
{
    return !m_missile_mergeBlock.IsConnected;
}

private bool HasCompletedLaunching()
{
    if (m_missile_launch_completed)
    {
        return true;
    }

    Vector3D missilePosition = GetMissileCurrentPosition();
    double launchedDistance = GetDistanceBetween(missilePosition, m_releasePosition);
    m_missile_launch_completed = launchedDistance >= LAUNCH_DISTANCE;

    return m_missile_launch_completed;
}

private void Launch()
{
    if (m_missile_forward_thrusts.Count() == 0)
    {
        LoadForwardThrusters();
    }

    SetThrustPercentage(1f);
}

private void SetThrustPercentage(float percentage)
{
    foreach (IMyThrust thrust in m_missile_forward_thrusts)
    {
        thrust.ThrustOverridePercentage = percentage;
    }
}

private void DisableThrustOverride()
{    foreach (IMyThrust thrust in m_missile_forward_thrusts)
    {
        thrust.ThrustOverride = 0f;
    }
}

private void ArmWarheads()
{
    GridTerminalSystem.GetBlocksOfType(m_missile_warheads);
    foreach (IMyWarhead warhead in m_missile_warheads)
    {
        warhead.IsArmed = true;
    }
}

private void LoadForwardThrusters()
{
    List<IMyTerminalBlock> forwardThrusts = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName("Forward", forwardThrusts, block => block is IMyThrust);
    Echo($"Found {forwardThrusts.Count()} forward thrusts");
    foreach (IMyTerminalBlock block in forwardThrusts)
    {
        m_missile_forward_thrusts.Add(block as IMyThrust);
    }
}

private Vector3D GetMissileCurrentPosition()
{
    return m_missile_remoteControl.CenterOfMass;
}

private bool TryGetTargetPosition(string timestamp, out Vector3D position)
{
    string customData = Me.CustomData;
    if (string.IsNullOrEmpty(customData))
    {
        PrintOnMotherShipScreen($"[{timestamp}]\r\nNo target info on Missile.");
        position = Vector3D.Zero;
        return false;
    }
    
    if (Vector3D.TryParse(customData, out position))
    {
        double distance = GetDistanceBetween(position, GetMissileCurrentPosition());
        string lockedInfo =
            $"[{timestamp}]\r\nMissile locked on target\r\nDistance = {Math.Round(distance, 3)}meter";
        PrintOnMotherShipScreen(lockedInfo);
        return true;
    }
    else
    {
        PrintOnMotherShipScreen($"[{timestamp}]\r\nCannot parse target info on missile: '{customData}'");
        position = Vector3D.Zero;
        return false;
    }
}

private void PrintOnLcdOnMissile(string message)
{
    // disable LCD on missile for now
    //SetLCDText(m_missile_lcd, message, Color.DarkGreen);
}


#region setup

private bool HasBeenSetup()
{
    if (m_missile_mergeBlock == null)
    {
        Echo("Not found merge block");
        return false;
    }
    if (m_missile_gyro == null)
    {
        Echo("Not found Gyroscope");
        return false;
    }
    if (m_missile_remoteControl == null)
    {
        Echo("Not found remote control");
        return false;
    }
    return true;
}

private void Setup()
{
    var groups = new List<IMyBlockGroup>();
    GridTerminalSystem.GetBlockGroups(groups);

    foreach (IMyBlockGroup g in groups)
    {
        if (g.Name.ToLower().Contains(MISSILE_GROUP_TAG.ToLower()))
        {
            var blocks = new List<IMyTerminalBlock>();
            g.GetBlocks(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.EntityId == Me.EntityId)
                {
                    SetupBlocks(blocks);
                    return;
                }
            }
        }
    }
}

private void SetupBlocks(List<IMyTerminalBlock> blocks)
{
    foreach (IMyTerminalBlock block in blocks)
    {
        if (block is IMyTextSurface)
        {
            //m_missile_lcd = block as IMyTextSurface;
        }
        else if (block is IMyGyro)
        {
            m_missile_gyro = block as IMyGyro;
        }
        else if (block is IMyRemoteControl)
        {
            m_missile_remoteControl = block as IMyRemoteControl;
        }
        else if (block is IMyShipMergeBlock)
        {
            m_missile_mergeBlock = block as IMyShipMergeBlock;
        }
    }
}

#endregion

#region debug_printing
const string BLOCKNAME_LCD_MotherShip = "Corax_LCD_MissileTargetInfo";

IMyTextSurface m_debugScreen_1 = null;

private void PrintOnMotherShipScreen(string message)
{
    if (m_debugScreen_1 == null)
    {
        m_debugScreen_1 = GridTerminalSystem.GetBlockWithName(BLOCKNAME_LCD_MotherShip) as IMyTextSurface;
    }
    
    if (m_debugScreen_1 != null)
    {
        SetLCDText(m_debugScreen_1, message, Color.DarkGreen);
    }
}

#endregion

#region util

private double GetDistanceBetween(Vector3D pointA, Vector3D pointB)
{
    double ax = pointA.X;
    double ay = pointA.Y;
    double az = pointA.Z;
    
    double bx = pointB.X;
    double by = pointB.Y;
    double bz = pointB.Z;

    return Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2) + Math.Pow(az - bz, 2));
}

private void SetLCDText(IMyTextSurface lcd, string text, Color color)
{
    lcd.FontColor = color;
    lcd.WriteText(text, false);
}

#endregion
