namespace Signal.Beacon.Core.Devices
{
    public record DeviceTarget : DeviceContactTarget
    {
        public string Channel { get; }

        public DeviceTarget(string channel, string identifier, string contact) : base(identifier, contact)
        {
            this.Channel = channel;
        }

        public void Deconstruct(out string channel, out string identifier, out string contact)
        {
            channel = this.Channel;
            identifier = this.Identifier;
            contact = this.Contact;
        }
    }
}