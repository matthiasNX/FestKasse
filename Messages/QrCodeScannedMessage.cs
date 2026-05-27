using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FestKasse.Messages;

public class QrCodeScannedMessage : ValueChangedMessage<string>
{
    public QrCodeScannedMessage(string value) : base(value) { }
}
