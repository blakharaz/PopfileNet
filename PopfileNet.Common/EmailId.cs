namespace PopfileNet.Common;

public record EmailId(uint Validity, uint Id)
{
    public override int GetHashCode()
    {
        unchecked
        {
            return (Validity.GetHashCode() * 397) ^ Id.GetHashCode();
        }
    }
}