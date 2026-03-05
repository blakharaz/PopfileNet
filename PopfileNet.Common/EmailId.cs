namespace PopfileNet.Common;

public class EmailId
{
    public uint Validity { get; }
    public uint Id { get; }

    public EmailId(uint validity, uint id)
    {
        Validity = validity;
        Id = id;
    }
    
    public EmailId(string str)
    {
        var parts = str.Split(':');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid EmailId format. Expected 'Validity:Id'.");
        }

        if (!uint.TryParse(parts[0], out var validity))
        {
            throw new FormatException("Invalid Validity value in EmailId.");
        }

        if (!uint.TryParse(parts[1], out var id))
        {
            throw new FormatException("Invalid Id value in EmailId.");
        }

        Validity = validity;
        Id = id;
    }
    
    public override string ToString()
    {
        return $"{Validity}:{Id}";
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Validity.GetHashCode() * 397) ^ Id.GetHashCode();
        }
    }
}