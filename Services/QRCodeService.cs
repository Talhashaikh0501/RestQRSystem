using QRCoder;

namespace RestaurantQR.Services
{
    public class QRCodeService
    {
        public byte[] GenerateQRCode(string content)
        {
            using var qrGenerator = new QRCodeGenerator();

            using var qrData = qrGenerator.CreateQrCode(
                content,
                QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrData);

            return qrCode.GetGraphic(20);
        }
    }
}