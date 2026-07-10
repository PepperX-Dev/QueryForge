namespace PepperX.QueryForge;

/// <summary>Defines the behavior of the validation engine when it encounters invalid properties.</summary>
public enum QueryValidationMode
{
    /// <summary>Silently removes (strips) invalid columns or clamps invalid pagination values in-place.</summary>
    SilentStrip,

    /// <summary>Throws a <see cref="QueryValidationException"/> immediately upon encountering any invalid property.</summary>
    ThrowException
}