using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PgpCore;

namespace Ex {
	[System.Flags]
	public enum Combo {
		None = 0, 
		Foo = 1, 
		Bar = 2, 
		FooBar = Foo | Bar,
		Bix = 4, 
		Baz = 8
	}

	/// <summary> Wrapper around <see cref="PgpCore.PGP"/> to make it easier to operate. </summary>
	public class Pgp {
		Combo c = Combo.Bar | Combo.Foo;
		/// <summary> Class holding a pair of public/private keys </summary>
		public sealed class KeyPair {
			/// <summary> Public key </summary>
			public string publicKey { get; private set; }
			/// <summary> Private key </summary>
			public string privateKey { get; private set; }
			/// <summary> Create a KeyPair with both public/private parts. </summary>
			/// <param name="publicKey"></param>
			/// <param name="privateKey"></param>
			public KeyPair(string publicKey, string privateKey) {
				this.publicKey = publicKey;
				this.privateKey = privateKey;
			}
			/// <summary> Create a "KeyPair" with just a public key </summary>
			/// <param name="publicKey"> Public key to use </param>
			/// <returns> "KeyPair" with just a public key </returns>
			public static KeyPair FromPublic(string publicKey) {
				return new KeyPair(publicKey, null);
			}
			/// <summary> Create a "KeyPair" with just a private key </summary>
			/// <param name="privateKey"> Private key to use </param>
			/// <returns> "KeyPair" with just a private key </returns>
			public static KeyPair FromPrivate(string privateKey) {
				return new KeyPair(null, privateKey);
			}
		}

		/// <summary> Instance of <see cref="PgpCore.PGP"/> because for whatever reason they threw their methods inside of it. </summary>
		private static readonly PGP instance = new PGP();

		/// <summary> Generate a public/private <see cref="KeyPair"/>. </summary>
		/// <param name="username"> Username bound to keypair </param>
		/// <param name="password"> Password bound to keypair </param>
		/// <param name="strength"> Encryption strength, defaults to 1024. </param>
		/// <param name="certainty"> Encryption certainty, defaults to 8</param>
		/// <returns> <see cref="KeyPair"/> with newly generated public/private keys. </returns>
		public static KeyPair GenerateKey(string username = "none", string password = "none", int strength = 1024, int certainty = 8) {
			Guid id = Guid.NewGuid();
			MemoryStream publicStream = new MemoryStream();
			MemoryStream privateStream = new MemoryStream();

			instance.GenerateKey(publicStream, privateStream, username, password, strength, certainty);

			string publicKey = Encoding.UTF8.GetString(publicStream.ToArray()).Replace("\r\n", "\n");
			string privateKey = Encoding.UTF8.GetString(privateStream.ToArray()).Replace("\r\n", "\n");

			return new KeyPair(publicKey, privateKey);
		}

		/// <summary> Encrypt the given <paramref name="payload"/> with the given <paramref name="publicKey"/>. </summary>
		/// <param name="payload"> Payload to encrypt </param>
		/// <param name="publicKey"> Key to use </param>
		/// <returns> Encrypted string </returns>
		public static string Encrypt(string payload, string publicKey) {
			MemoryStream ins = new MemoryStream(payload.ToBytesUTF8());
			MemoryStream outs = new MemoryStream();
			MemoryStream keys = new MemoryStream(publicKey.ToBytesUTF8());

			DateTime start = DateTime.UtcNow;
			instance.EncryptStream(ins, outs, keys, true, true);
			DateTime end = DateTime.UtcNow;
			Log.Debug($"Pgp.Encrypt took {(end-start).TotalMilliseconds}ms");
			return Encoding.UTF8.GetString(outs.ToArray()).Replace("\r\n", "\n");
		}
		/// <summary> Encrypt the given <paramref name="payload"/> with the given <paramref name="keyPair"/>. </summary>
		/// <param name="payload"> Payload to encrypt </param>
		/// <param name="keyPair"> Keypair to verify with (uses <see cref="KeyPair.publicKey"/>) </param>
		/// <returns> Encrypted string </returns>
		public static string Encrypt(string payload, KeyPair keyPair) { 
			return Encrypt(payload, keyPair.publicKey);
		}

		/// <summary> Sign the given <paramref name="payload"/> with the given <paramref name="privateKey"/>. </summary>
		/// <param name="payload"> Payload to sign </param>
		/// <param name="privateKey"> Key to use </param>
		/// <returns> Signed string </returns>
		public static string Sign(string payload, string privateKey, string password = "none") {
			MemoryStream ins = new MemoryStream(payload.ToBytesUTF8());
			MemoryStream outs = new MemoryStream();
			MemoryStream keys = new MemoryStream(privateKey.ToBytesUTF8());

			DateTime start = DateTime.UtcNow;
			instance.SignStream(ins, outs, keys, password, true, true);
			DateTime end = DateTime.UtcNow;
			Log.Debug($"Pgp.Sign took {(end - start).TotalMilliseconds}ms");

			return Encoding.UTF8.GetString(outs.ToArray()).Replace("\r\n", "\n");
		}
		/// <summary> Sign the given <paramref name="payload"/> with the given <paramref name="keyPair"/>. </summary>
		/// <param name="payload"> Payload to sign </param>
		/// <param name="keyPair"> Keypair to verify with (uses <see cref="KeyPair.privateKey"/>) </param>
		/// <returns> Signed string </returns>
		public static string Sign(string payload, KeyPair keyPair, string password = "none") {
			return Sign(payload, keyPair.privateKey, password);
		}

		/// <summary> Encrypt and sign the given <paramref name="payload"/> with the given <paramref name="publicKey"/> and <paramref name="privateKey"/>. </summary>
		/// <param name="payload"> Payload to encrypt/sign</param>
		/// <param name="publicKey"> Public key to encrypt with </param>
		/// <param name="privateKey"> Private key to sign with </param>
		/// <param name="password"> password to use.</param>
		/// <returns> Encrypted and signed payload. </returns>
		public static string EncryptAndSign(string payload, string publicKey, string privateKey, string password = "none") {
			MemoryStream ins = new MemoryStream(payload.ToBytesUTF8());
			MemoryStream outs = new MemoryStream();
			MemoryStream publicKeys = new MemoryStream(publicKey.ToBytesUTF8());
			MemoryStream privateKeys = new MemoryStream(privateKey.ToBytesUTF8());

			DateTime start = DateTime.UtcNow;
			instance.EncryptStreamAndSign(ins, outs, publicKeys, privateKeys, password, true, true);
			DateTime end = DateTime.UtcNow;
			Log.Debug($"Pgp.EncryptAndSign took {(end - start).TotalMilliseconds}ms");

			return Encoding.UTF8.GetString(outs.ToArray()).Replace("\r\n", "\n");
		}
		/// <summary> Decrypt the given <paramref name="encrypted"/> payload with the given <paramref name="privateKey"/>. </summary>
		/// <param name="payload"> Payload to decrypt </param>
		/// <param name="privateKey"> Key to use </param>
		/// <returns> Decrypted string </returns>
		public static string Decrypt(string encrypted, string privateKey, string password = "none") {
			MemoryStream ins = new MemoryStream(encrypted.ToBytesUTF8());
			MemoryStream outs = new MemoryStream();
			MemoryStream keys = new MemoryStream(privateKey.ToBytesUTF8());

			DateTime start = DateTime.UtcNow;
			instance.DecryptStream(ins, outs, keys, password);
			DateTime end = DateTime.UtcNow;
			Log.Debug($"Pgp.Decrypt took {(end - start).TotalMilliseconds}ms");
			
			return Encoding.UTF8.GetString(outs.ToArray()).Replace("\r\n", "\n");
		}
		/// <summary> Decrypt the given <paramref name="encrypted"/> payload with the given <paramref name="keyPair"/>. </summary>
		/// <param name="payload"> Payload to decrypt </param>
		/// <param name="keyPair"> Keypair to verify with (uses <see cref="KeyPair.privateKey"/>) </param>
		/// <returns> Decrypted string </returns>
		public static string Decrypt(string encrypted, KeyPair keyPair, string password = "none") {
			return Decrypt(encrypted, keyPair.privateKey, password);
		}

		/// <summary> Verify the given <paramref name="encrypted"/> payload was signed with private key that matches <paramref name="publicKey"/></summary>
		/// <param name="encrypted"> Encrypted payload to verify </param>
		/// <param name="publicKey"> Public key to verify with </param>
		/// <returns> True, if verified, or false if not. </returns>
		public static bool Verify(string encrypted, string publicKey) {
			MemoryStream ins = new MemoryStream(encrypted.ToBytesUTF8());
			MemoryStream keys = new MemoryStream(publicKey.ToBytesUTF8());

			return instance.VerifyStream(ins, keys);
		}
		/// <summary> Verify the given <paramref name="encrypted"/> payload was signed with the given <paramref name="keyPair"/>. </summary>
		/// <param name="encrypted"> Encrypted payload to verify </param>
		/// <param name="keyPair"> Keypair to verify with (uses <see cref="KeyPair.publicKey"/>) </param>
		/// <returns> True, if verified, or false if not. </returns>
		public static bool Verify(string encrypted, KeyPair keyPair) {
			return Verify(encrypted, keyPair.publicKey);
		}

		/// <summary> Both decrypt and verify the given <paramref name="encrypted"/> payload.
		/// Decrypt it with the given <paramref name="privateKey"/>, 
		/// and verify that it was signed with the private key that matches <paramref name="publicKey"/> </summary>
		/// <param name="encrypted"> Encrypted payload to decrypt and verify </param>
		/// <param name="publicKey"> Public key to verify signature with </param>
		/// <param name="privateKey"> Private key to decrypt with </param>
		/// <param name="result"> place to store output </param>
		/// <param name="password"> optional password to use </param>
		/// <returns> True, if decryption and verification were successful. False otherwise. </returns>
		public static bool DecryptAndVerify(string encrypted, string publicKey, string privateKey, out string result, string password = "none") {
			MemoryStream ins = new MemoryStream(encrypted.ToBytesUTF8());
			MemoryStream outs = new MemoryStream();
			MemoryStream publicKeys = new MemoryStream(publicKey.ToBytesUTF8());
			MemoryStream privateKeys = new MemoryStream(privateKey.ToBytesUTF8());

			try {
				instance.DecryptStreamAndVerify(ins, outs, publicKeys, privateKeys, password);
				result = Encoding.UTF8.GetString(outs.ToArray()).Replace("\r\n", "\n");
				return true;
			} catch (Exception) {
				result = null;
				return false;
			}
		}

	}

	public static class Pgp_Tests {
		public static void TestStuff() {
			//Console.WriteLine("Testing key generation.");
			Pgp.KeyPair kpa = Pgp.GenerateKey();
			//Console.WriteLine($"PublicKey:\n{kpa.publicKey}");
			//Console.WriteLine($"PrivateKey:\n{kpa.privateKey}");
			Pgp.KeyPair kpb = Pgp.GenerateKey();

			string secret = "Oh yeet ｕｈｍ ｙｅｅｅｔ 千ㄩ㇄㇄山工ᗪㄒ廾";
			//Console.WriteLine($"Testing encryption: \"{secret}\"");

			string encrypted = Pgp.Encrypt(secret, kpa);
			//Console.WriteLine($"Encrypted as:\n{encrypted}");

			string signed = Pgp.Sign(secret, kpa);
			//Console.WriteLine($"Signed as:\n{signed}");

			string decrypted = Pgp.Decrypt(encrypted, kpa);
			//Console.WriteLine($"Decrpyted message: {decrypted}");
			if (decrypted != secret) { throw new Exception("Pgp_Tests: Encryption/Decryption failed"); }
			if (!Pgp.Verify(signed, kpa)) { throw new Exception("Pgp_Tests: Sign/Verify failed");}
			//Console.WriteLine($"Verify signed message: {Pgp.Verify(signed, kpa)}");

			string encryptedAndSigned = Pgp.EncryptAndSign(secret, kpa.publicKey, kpb.privateKey);
			//Console.WriteLine($"Asymetrically encrypted and signed:\n{encryptedAndSigned}");

			string decryptedAndVerified;
			if (Pgp.DecryptAndVerify(encryptedAndSigned, kpb.publicKey, kpa.privateKey, out decryptedAndVerified)) {
				//Console.WriteLine($"Decrypted and verified message: {decryptedAndVerified}");
			} else {
				throw new Exception("Pgp_Tests: DecryptAndVerify failed!");
			}
			string cannedString = @"-----BEGIN PGP MESSAGE-----
Version: BCPG C# v1.8.8.0

hIwD6x2om9L5x94BA/4qH6xwB5Z09I85LpqmKVQJHozs5mYxWIpT9FMy207Kbnk/
aULhR52dX7GCPLqZkaz2QzzRrJOzSB9C/g2cAvB6eq6Q7+dr22AsFZkDhzg0s04j
7njj1fxKYO/nBUcE+w9jqEXm/C/3tkY44GnmSYPeBnL2HdoqtSeAnZjLI3924NLA
XQGSluiRItEIrNNa3DU0Bn5i+zYQZZCcCqWr2HnCruS8/D/jwwNo/UixoL36GfiZ
MzOK14+a98Wuoniiq3OKcAVMON7ivuEaYhsoTq+P8jLhfS1hmN32gaA4nbdXG/wu
n2ZmnibWYS8ezkrrztsAZubrIRjO9dtqnX1sWI+gLr2j75jageV2K1QNZrxaST2b
WMX92WZJ6uaq8MZ58ljbzo1IJrONw0ljINp90h8fXk/KfTfJiZcGLtSFshX2cjkR
JjTJ5lyWXsXaNW+3LVOb1J1ZNkKYUvDzDT14S5C/bxPQ52CxnBi/2t+EREKZGVW/
/2htRaAL65hqcgleOIfCU0oN5UbTk6kdppr7vC9yMqDGpc2j7KI6FX0INeNrCg==
=hlqF
-----END PGP MESSAGE-----";
			if (Pgp.DecryptAndVerify(cannedString, kpb.publicKey, kpa.privateKey, out decryptedAndVerified)) {
				throw new Exception("Pgp_Tests: DecryptAndVerify succeeded on bad data! Not good - either you won the lottery or there's a bug!");
			} else {
				//Console.WriteLine("DecryptAndVerify failed successfully on mutated data!");
			}

		}
	}

}
