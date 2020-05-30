const string AIMINGSYS_TAG = "AimingSys";
const string HORIZONTAL_ROTOR_TAG = "horizontal";
const string VERTICAL_ROTOR_TAG = "vertical";
const string NAME_TAG_PROGRAMBLOCK_ON_MISSILE = "NAIL";

const double verticalSpeedModifier = -0.32;

const double horizontalSpeedModifier = 0.32;

const double LOCK_DISTANCE = 10000;

private IMyShipController m_myShipController = null;
private IMyMotorStator m_horizontalRotor = null;
private IMyMotorStator m_verticalRotor = null;
private IMyCameraBlock m_cameraBlock = null;
private List<IMyGyro> m_gyros = new List<IMyGyro>();
private List<IMyTerminalBlock> programmablesOnMissiles = new List<IMyTerminalBlock>();

private bool m_cameraActiveLastRun = false;

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
    
    if (!HasBeenSetup())
    {
        PrintWarning($"[{timestamp}]\r\nAiming System is offline.");
        Setup();
    }
    else
    {
        Aiming(timestamp);
    }
}

private void Aiming(string timestamp)
{
    Echo("Aiming");

    if (!m_cameraBlock.IsActive)
    {
        Print($"[{timestamp}]\r\nAiming System is online.");
        m_cameraActiveLastRun = false;
        StopRotors();
        return;
    }
    
    if (!m_cameraActiveLastRun)
    {
        // start aiming
        ResetTargetInfo();
        m_cameraActiveLastRun = true;

        ActivateRotors();
    }
    
    Vector2 mouseMovement = m_myShipController.RotationIndicator;
    double horizontalV = mouseMovement.Y * horizontalSpeedModifier;
    double verticalV = mouseMovement.X * verticalSpeedModifier;
    
    ApplyMouseMovement(horizontalV, verticalV);
    OverrideGyro(true);
    
    Vector3D targetPosition = LockOnTarget(timestamp);
    
    if (!targetPosition.Equals(Vector3D.Zero))
    {
        SetTargetOnMissiles(targetPosition);
    }
}

private void SetTargetOnMissiles(Vector3D targetPosition)
{
    programmablesOnMissiles.Clear();
    GridTerminalSystem.SearchBlocksOfName(NAME_TAG_PROGRAMBLOCK_ON_MISSILE, programmablesOnMissiles, b => b is IMyProgrammableBlock);
    foreach (IMyProgrammableBlock p in programmablesOnMissiles)
    {
        p.CustomData = targetPosition.ToString();
    }
}

private Vector3D LockOnTarget(string timestamp)
{
    m_cameraBlock.EnableRaycast = true;
    double distance = LOCK_DISTANCE;
    float pitch = 0f;
    float yaw = 0f;
    MyDetectedEntityInfo target = m_cameraBlock.Raycast(distance, pitch, yaw);
    if (target.Type != MyDetectedEntityType.None && target.HitPosition.HasValue)
    {
        double tx = target.HitPosition.Value.X;
        double ty = target.HitPosition.Value.Y;
        double tz = target.HitPosition.Value.Z;

        Vector3D myShipCenter = m_myShipController.CenterOfMass;
        double mx = myShipCenter.X;
        double my = myShipCenter.Y;
        double mz = myShipCenter.Z;

        double distanceToTarget = Math.Sqrt(Math.Pow(tx - mx, 2) + Math.Pow(ty - my, 2) + Math.Pow(tz - mz, 2));
        PrintTargetInfo($"[{timestamp}]\r\nLOCK ON\r\nName: {target.Name}\r\nType = {target.Type}\r\nRelationship = {target.Relationship}\r\nDistance = {Math.Round(distanceToTarget, 3)}meter");
        return target.HitPosition.Value;
    }
    else
    {
        return Vector3D.Zero;
    }
}



private void ApplyMouseMovement(double horizontalV, double verticalV)
{
    m_horizontalRotor.TargetVelocityRPM = (float)horizontalV;
    m_verticalRotor.TargetVelocityRPM = (float)verticalV;
}

private void OverrideGyro(bool gyroOverride)
{
    foreach (IMyGyro gyro in m_gyros)
    {
        if (gyroOverride)
        {
            gyro.Pitch = 0f;
            gyro.Yaw = 0f;
            gyro.Roll = 0f;
        }
        gyro.GyroOverride = gyroOverride;
    }
}

private void Setup()
{
    Echo("Setting up...");

    var groups = new List<IMyBlockGroup>();
    GridTerminalSystem.GetBlockGroups(groups);

    foreach (IMyBlockGroup g in groups)
    {
        if (g.Name.ToLower().Contains(AIMINGSYS_TAG.ToLower()))
        {
            SetupBlocks(g);
        }
    }
    
    m_gyros.Clear();
    GridTerminalSystem.GetBlocksOfType(m_gyros);
}

private void SetupBlocks(IMyBlockGroup g)
{
        var blocks = new List<IMyTerminalBlock>();
        g.GetBlocks(blocks);
        
        foreach (IMyTerminalBlock block in blocks)
        {
            if (block is IMyShipController)
            {
                SetupShipController(block as IMyShipController);
            }
            else if (block is IMyMotorStator)
            {
                if (block.CustomName.ToLower().Contains(HORIZONTAL_ROTOR_TAG))
                {
                    m_horizontalRotor = block as IMyMotorStator;
                }
                else if (block.CustomName.ToLower().Contains(VERTICAL_ROTOR_TAG))
                {
                    m_verticalRotor = block as IMyMotorStator;
                }
            }
            else if (block is IMyCameraBlock)
            {
                m_cameraBlock = block as IMyCameraBlock;
            }
        }
}

private void SetupShipController(IMyShipController sc)
{
    if (sc.IsUnderControl && sc.CanControlShip)
    {
        m_myShipController = sc;
        Echo($"Setup ShipController");
    }
    else
    {
        Echo($"Cannot setup ShipController because it is not under control");
    }
}

private void ResetTargetInfo()
{
    PrintTargetInfo(string.Empty);
}

private void ActivateRotors()
{
    m_horizontalRotor.RotorLock = false;
    m_verticalRotor.RotorLock = false;
}

private void StopRotors()
{
    OverrideGyro(false);
    m_horizontalRotor.TargetVelocityRPM = 0f;
    m_horizontalRotor.RotorLock = true;
    m_verticalRotor.TargetVelocityRPM = 0f;
    m_verticalRotor.RotorLock = true;
}

private bool HasBeenSetup()
{
    return m_myShipController != null
        && m_horizontalRotor != null
        && m_verticalRotor != null
        && m_cameraBlock != null;
}

#region printing_utils

const string NAME_LCD_AIMINGSYS_STATUS = "Corax_LCD_AimingSys";
const string NAME_LCD_TARGET_INFO = "Corax_LCD_TargetInfo";

private IMyTextSurface m_lcd_aimingSys = null;
private IMyTextSurface m_lcd_targetInfo = null;

private void PrintTargetInfo(string message)
{
    if (m_lcd_targetInfo == null)
        m_lcd_targetInfo = GridTerminalSystem.GetBlockWithName(NAME_LCD_TARGET_INFO) as IMyTextSurface;
    if (m_lcd_targetInfo != null)
        SetLCDText(m_lcd_targetInfo, message, Color.DarkGreen);
}

private void Print(string message)
{
    if (m_lcd_aimingSys == null)
        m_lcd_aimingSys = GridTerminalSystem.GetBlockWithName(NAME_LCD_AIMINGSYS_STATUS) as IMyTextSurface;
    if (m_lcd_aimingSys != null)
        SetLCDText(m_lcd_aimingSys, message, Color.DarkGreen);
}

private void PrintWarning(string message)
{
    if (m_lcd_aimingSys == null)
        m_lcd_aimingSys = GridTerminalSystem.GetBlockWithName(NAME_LCD_AIMINGSYS_STATUS) as IMyTextSurface;
    if (m_lcd_aimingSys != null)
        SetLCDText(m_lcd_aimingSys, message, Color.DarkGoldenrod);
}

private void PrintError(string message)
{
    if (m_lcd_aimingSys == null)
        m_lcd_aimingSys = GridTerminalSystem.GetBlockWithName(NAME_LCD_AIMINGSYS_STATUS) as IMyTextSurface;
    if (m_lcd_aimingSys != null)
        SetLCDText(m_lcd_aimingSys, message, Color.DarkRed);
}

private void SetLCDText(IMyTextSurface lcd, string text, Color color)
{
    lcd.FontColor = color;
    lcd.WriteText(text, false);
}

#endregion
