
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Source.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService()
        {
            string cloudName =
                ConfigurationManager.AppSettings["CloudinaryCloudName"];

            string apiKey =
                ConfigurationManager.AppSettings["CloudinaryApiKey"];

            string apiSecret =
                ConfigurationManager.AppSettings["CloudinaryApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new ConfigurationErrorsException(
                    "Thieu cau hinh Cloudinary trong Web.config.");
            }

            Account account = new Account(
                cloudName,
                apiKey,
                apiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public string UploadPdf(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new ArgumentException(
                    "Vui long chon file PDF.");
            }

            string extension =
                Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Chi chap nhan file PDF.");
            }

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(
                    Path.GetFileName(file.FileName),
                    file.InputStream
                ),

                PublicId = "syllabus/" + Guid.NewGuid()
            };

            var result = _cloudinary.Upload(
         uploadParams,
         "application/pdf"
     );

            if (result == null || result.Error != null)
            {
                string message = result?.Error?.Message
                    ?? "Khong the upload PDF len Cloudinary.";

                throw new Exception(message);
            }

            return result.SecureUrl.ToString();
        }
    }
}