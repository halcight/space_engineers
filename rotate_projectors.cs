private const string TAG = "AutoRotate";

private TimeSpan m_searchInterval = TimeSpan.FromSeconds(30);
private List<IMyProjector> m_rotateProjectors = new List<IMyProjector>();
private DateTime m_lastSearchTime = DateTime.MinValue;

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
	
	Echo($"[{DateTime.Now}]");
	
	var projectors = GetAllRotateProjectors();
	
	foreach (var projector in projectors)
	{
		Rotate(projector);
	}
}

private void Rotate(IMyProjector projector)
{
	Vector3I rotation = projector.ProjectionRotation;
	
	string displayNameText = projector.DisplayNameText;
	if (displayNameText.Contains("Pitch"))
	{
		rotation.X++;
		if (rotation.X >= 180)
			rotation.X = -180;
	}
	else if (displayNameText.Contains("Yall"))
	{
		rotation.Y++;
		if (rotation.Y >= 180)
			rotation.Y = -180;
	}
	else if (displayNameText.Contains("Roll"))
	{
		rotation.Z++;
		if (rotation.Z >= 180)
			rotation.Z = -180;
	}
	else
	{
		return;
	}
	
	projector.ProjectionRotation = rotation;
	projector.UpdateOffsetAndRotation();
}

private List<IMyProjector> GetAllRotateProjectors()
{
	if (m_rotateProjectors.Count > 0 && DateTime.Now.Subtract(m_lastSearchTime) < m_searchInterval)
	{
		return m_rotateProjectors;
	}
	
	m_rotateProjectors.Clear();
	
	GridTerminalSystem.GetBlocksOfType(m_rotateProjectors, projector => projector.DisplayNameText.Contains(TAG));
	
	m_lastSearchTime = DateTime.Now;
	
	return m_rotateProjectors;
}