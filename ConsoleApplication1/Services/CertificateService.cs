using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NLog;

namespace HttpLogger.Services
{
    /// <summary>
    /// Defines the <see cref="CertificateService"/> class which is used to handle operations with Certificates.
    /// </summary>
    public class CertificateService
    {
        /// <summary>
        /// The constant value defining the issuer name signed on all public keys and private keys.
        /// </summary>
        private const string ISSUER_NAME = "DO_NOT_TRUST_HTTP_Logger_Root";

        /// <summary>
        /// The current classes <see cref="ILogger"/> implementation.
        /// </summary>
        private static ILogger _nLogger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Retrieves a self signed cert for the subject name provided by the subject uri, from the specified store.
        /// If the self signed cert doesn't exist, this method will attempt to create one and store it in the specified store. 
        /// </summary>
        /// <param name="subjectUri"><see cref="Uri"/> object the subject name</param>
        /// <param name="issuerPrivKey"><see cref="AsymmetricKeyParameter"/> object of the issuer private key</param>
        /// <param name="storeName">
        /// The <see cref="StoreName"/> of where the certificate is stored. 
        /// Default: StoreName.My
        /// </param>
        /// <param name="storeLocation">
        /// The <see cref="StoreLocation"/> of where the certificate is stored.
        /// Default: StoreLocation.CurrentUser
        /// </param>
        /// <returns>
        /// Returns a <see cref="X509Certificate2"/>instance of the certificate.
        /// </returns>
        public static X509Certificate2 GetSelfSignedCertificate(Uri subjectUri, AsymmetricKeyParameter issuerPrivKey, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            X509Certificate2 cert = null;
            var store = new X509Store(storeName, storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                var results = store.Certificates.Find(X509FindType.FindBySubjectName, $"*.{subjectUri.DnsSafeHost}", false);
                results = results.Find(X509FindType.FindByIssuerName, ISSUER_NAME, false);

                if (results.Count <= 0)
                {                    
                    return GenerateSelfSignedCertificate(subjectUri, ISSUER_NAME, issuerPrivKey);
                }

                cert = results[0];

            }
            catch (Exception ex)
            {
                _nLogger.Error(ex, $"Error retrieving self signed cert.");
            }
            finally
            {
                store.Close();
            }

            return cert;
        }

        /// <summary>
        /// Generates a root CA Certificate.
        /// </summary>
        /// <param name="subjectName">
        /// The name of the subject and issuer.
        /// Default: DO_NOT_TRUST_HTTP_Logger_Root</param>
        /// <param name="keyStrength">
        /// The strength of thet key. 
        /// Default: 2048
        /// </param>
        /// <returns></returns>
        public static AsymmetricKeyParameter GenerateCACertificate(string subjectName = ISSUER_NAME, int keyStrength = 2048)
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);
            
            // Issuer and Subject Name
            var subjectDN = new X509Name($"CN={subjectName}, O={subjectName}, OU=Created by http://httplogger.net");
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);            

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);
            
            var cert = new X509Certificate2(certificate.GetEncoded());

            var exported = cert.Export(X509ContentType.Pkcs12, "password");
            var pfxCert = new X509Certificate2(exported, "password");

            // Convert BouncyCastle Private Key to RSA
            var rsaPriv = DotNetUtilities.ToRSA(issuerKeyPair.Private as RsaPrivateCrtKeyParameters);

            // Setup RSACryptoServiceProvider with "KeyContainerName" set
            var csp = new CspParameters();
            csp.KeyContainerName = "KeyContainer";

            var rsaPrivate = new RSACryptoServiceProvider(csp);

            // Import private key from BouncyCastle's rsa
            rsaPrivate.ImportParameters(rsaPriv.ExportParameters(true));

            // Set private key on our X509Certificate2
            pfxCert.PrivateKey = rsaPrivate;
            
            // Add CA certificate to Root store
            if (!AddCertificateToStore(pfxCert, StoreName.Root, StoreLocation.CurrentUser) || !AddCertificateToStore(cert, StoreName.Root, StoreLocation.CurrentUser))
            {
                return null;
            }

            return issuerKeyPair.Private;

        }

        /// <summary>
        /// Generate a self signed certificate.
        /// </summary>
        /// <param name="subjectName"><see cref="Uri"/> object the subject name</param>
        /// <param name="issuerName"> The name of the issuer </param>
        /// <param name="issuerPrivKey"><see cref="AsymmetricKeyParameter"/> object of the issuer private key</param>
        /// <param name="keyStrength">
        /// The strength of thet key. 
        /// Default: 2048
        /// </param>
        /// <returns>
        /// Returns a <see cref="X509Certificate2"/>instance of the certificate.
        /// </returns>
        private static X509Certificate2 GenerateSelfSignedCertificate(Uri subjectName, string issuerName, AsymmetricKeyParameter issuerPrivKey, int keyStrength = 2048)
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            // Issuer and Subject Name
            var subjectDN = new X509Name($"CN=*.{subjectName.DnsSafeHost}, O={issuerName}, OU=Created by http://httplogger.net");
            var issuerDN = new X509Name($"CN={issuerName}, O={issuerName}, OU=Created by http://httplogger.net");
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            var subjectAlternativeNames = new Asn1Encodable[]
                {
                    new GeneralName(GeneralName.DnsName, $"{subjectName.DnsSafeHost}"),
                    new GeneralName(GeneralName.DnsName, $"*.{subjectName.DnsSafeHost}"),
                };
            var subjectAlternativeNamesExtension = new DerSequence(subjectAlternativeNames);
            certificateGenerator.AddExtension(
                X509Extensions.SubjectAlternativeName.Id, false, subjectAlternativeNamesExtension);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
            var certificate = certificateGenerator.Generate(issuerPrivKey, random);

            // correcponding private key
            var info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);


            // merge into X509Certificate2
            var x509 = new X509Certificate2(certificate.GetEncoded());

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(info.PrivateKey.GetDerEncoded());
            if (seq.Count != 9)
                throw new PemException("malformed sequence in RSA private key");

            var rsa = new RsaPrivateKeyStructure(seq);

            var rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);

            AddCertificateToStore(x509, StoreName.My, StoreLocation.CurrentUser);
            return x509;

        }

        /// <summary>
        /// Adds the certificate to the certificate store.
        /// </summary>
        /// <param name="cert">The <see cref="X509Certificate2"/> instance of the certificate to be added.</param>
        /// <param name="storeName">The <see cref="StoreName"/></param>
        /// <param name="storeLocation">The <see cref="StoreLocation"/></param>
        /// <returns>Returns a <see cref="bool"/> value indicating whether or not adding the certificate was succesful.</returns>
        private static bool AddCertificateToStore(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
        {
            var success = false;
            var store = new X509Store(storeName, storeLocation);
            try
            {

                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);

                success = true;
            }
            catch (Exception ex)
            {

                _nLogger.Error(ex, $"Error when adding certificate to the store.");
            }
            finally
            {
                store.Close();
            }

            return success;
        }
    }
}
