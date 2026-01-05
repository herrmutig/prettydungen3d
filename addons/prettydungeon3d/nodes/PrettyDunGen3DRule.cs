using Godot;

namespace PrettyDunGen3D;

/// <summary>
/// Base class for defining custom generation rules used by <see cref="PrettyDunGen3DGenerator"/>.
///
/// A rule represents a single step in the dungeon generation pipeline and is executed
/// sequentially during generation phase. Rules can modify chunks,
/// place geometry, validate constraints, or abort generation entirely.
///
/// To avoid editor errors it is highly recommended to add the <see cref="ToolAttribute"/>
/// to any class that inherits from <see cref="PrettyDunGen3DRule"/>
/// More Info: https://docs.godotengine.org/en/stable/tutorials/plugins/running_code_in_the_editor.html
/// </summary>
[Tool]
[GlobalClass]
public partial class PrettyDunGen3DRule : Node
{
    // Ugly, but it works.
    [Export]
    public bool Mute
    {
        get => mute;
        set
        {
            mute = value;

            if (value)
            {
                if (!Name.ToString().EndsWith("(MUTED)"))
                    Name += "(MUTED)";
                return;
            }

            if (Name.ToString().EndsWith("(MUTED)"))
                Name = Name.ToString()[..^7]; // Remove 7 characters.
        }
    }

    /// <summary>
    /// If enabled, dungeon generation stops when the rule fails.
    /// Otherwise, generation continues with the next rule.
    /// </summary>
    [Export]
    public bool StopDungeonGenerationOnError { get; set; } = true;

    private bool mute;

    /// <summary>
    /// Called once before generation begins.
    /// Use this to initialize or reset internal state.
    /// </summary>
    public virtual void OnInitialize(PrettyDunGen3DGenerator generator) { }

    /// <summary>
    /// Called during the global dungeon generation phase.
    /// Rules are executed sequentially according to their hierarchy order.
    /// Override this method to inject custom generation logic.
    /// </summary>
    /// <param name="generator">
    /// The active <see cref="PrettyDunGen3DGenerator"/> instance controlling the generation process.
    /// </param>
    /// <returns>
    /// Return <c>null</c> to allow generation to continue.
    /// Return a non-null <see cref="string"/> to immediately stop generation and output the returned message.
    /// </returns>
    public virtual string OnGenerate(PrettyDunGen3DGenerator generator)
    {
        GD.Print("Note: Override OnGenerate to create a custom rule!", this);
        return null;
    }

    /// <summary>
    /// Can be overwritten to create custom visual debugging information
    /// </summary>
    public virtual void DrawDebug() { }
}
