# Crypto 加密解密工具类

`Crypto` 是一个功能完整的加密解密工具类，提供了 AES、RSA 以及混合加密等多种加密方式，支持字符串、字节数组和文件的加密解密操作。

## 功能特性

- **AES 加密解密**：支持 256 位密钥的 AES 加密，使用 CBC 模式和 PKCS7 填充
- **RSA 加密解密**：支持 RSA 公钥加密和私钥解密，使用 OAEP-SHA256 填充
- **RSA 数字签名**：支持数据签名和验证，确保数据完整性和身份认证
- **混合加密**：结合 AES 和 RSA 的优势，提供高性能和高安全性的加密方案
- **文件加密**：支持直接对文件进行加密和解密操作
- **JSON 序列化**：加密数据支持 JSON 格式的序列化和反序列化

## 使用方法

### 1. AES 加密解密

#### 生成 AES 密钥
```csharp
// 生成 32 字节的 AES 密钥
byte[] aesKey = Crypto.GenerateAesKey();
```

#### 加密字节数组
```csharp
// 使用指定密钥加密
byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
byte[] key = Crypto.GenerateAesKey();
var (encryptData, returnedKey) = Crypto.AesEncrypt(data, key);

// 使用随机生成的密钥加密
var (encryptData2, randomKey) = Crypto.AesEncrypt(data, null);
```

#### 解密字节数组
```csharp
byte[] decryptedData = Crypto.AesDecrypt(encryptData, key);
string result = Encoding.UTF8.GetString(decryptedData);
```

#### 加密字符串
```csharp
string data = "Hello, World!";
string base64Key = Convert.ToBase64String(Crypto.GenerateAesKey());
var (encryptData, key) = Crypto.AesEncrypt(data, base64Key);
```

#### 解密字符串
```csharp
string decryptedString = Crypto.AesDecrypt(encryptData, base64Key);
```

#### 文件加密解密
```csharp
// 加密文件
string sourceFile = "source.txt";
string targetFile = "encrypted.txt";
string base64Key = Convert.ToBase64String(Crypto.GenerateAesKey());
var (encryptData, key) = Crypto.AesEncryptFile(sourceFile, targetFile, base64Key);

// 解密文件
string decryptFile = "decrypted.txt";
Crypto.AesDecryptFile(targetFile, decryptFile, base64Key);
```

### 2. RSA 加密解密

#### 生成 RSA 密钥对
```csharp
var (publicKey, privateKey) = Crypto.GenerateRsaKey();
```

#### 加密数据
```csharp
string data = "Hello, World!";
string encryptedData = Crypto.RsaEncrypt(data, publicKey);
```

#### 解密数据
```csharp
string decryptedData = Crypto.RsaDecrypt(encryptedData, privateKey);
```

#### 字节数组加密解密
```csharp
byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
byte[] encryptedBytes = Crypto.RsaEncrypt(data, publicKey);
byte[] decryptedBytes = Crypto.RsaDecrypt(encryptedBytes, privateKey);
```

### 3. RSA 数字签名

#### 数据签名
```csharp
string data = "Hello, World!";
string signature = Crypto.RsaSignData(data, privateKey);
```

#### 验证签名
```csharp
bool isValid = Crypto.RsaVerifyData(data, signature, publicKey);
```

#### 字节数组签名验证
```csharp
byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
byte[] signature = Crypto.RsaSignData(data, privateKey);
bool isValid = Crypto.RsaVerifyData(data, signature, publicKey);
```

### 4. 混合加密（AES + RSA）

混合加密结合了 AES 的高性能和 RSA 的安全性，适用于需要高性能和高安全性的场景。

#### 加密流程
1. 使用自己的 RSA 私钥对数据进行签名
2. 使用随机生成的 AES 密钥加密数据
3. 使用对方的 RSA 公钥加密 AES 密钥

```csharp
string data = "Hello, World!";
var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
var (facePublicKey, facePrivateKey) = Crypto.GenerateRsaKey();

// 加密
string hybridJson = Crypto.HybridEncrypt(data, ownPrivateKey, facePublicKey);
```

#### 解密流程
1. 使用自己的 RSA 私钥解密 AES 密钥
2. 使用 AES 密钥解密数据
3. 使用对方的 RSA 公钥验证数据签名

```csharp
// 解密
string decryptedData = Crypto.HybridDecrypt(hybridJson, facePrivateKey, ownPublicKey);
```

#### 字节数组混合加密
```csharp
byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
HybridEncryptData hybridData = Crypto.HybridEncrypt(data, ownPrivateKey, facePublicKey);
byte[] decryptedData = Crypto.HybridDecrypt(hybridData, facePrivateKey, ownPublicKey);
```

## 数据结构

### AesEncryptData
AES 加密后的数据结构，包含加密数据和初始化向量（IV）。

```csharp
public class AesEncryptData
{
    public byte[] Iv { get; set; }           // 初始化向量（16字节）
    public byte[] CipherData { get; set; }   // 加密后的数据
    
    public string GetIvString()              // 获取IV的Base64字符串
    public string GetCipherDataString()      // 获取加密数据的Base64字符串
    public string ToJson()                   // 序列化为JSON
    public static AesEncryptData FromJson(string json)  // 从JSON反序列化
}
```

### HybridEncryptData
混合加密后的数据结构，包含 AES 加密数据、签名和 RSA 加密的 AES 密钥。

```csharp
public class HybridEncryptData
{
    public AesEncryptData AesEncryptData { get; set; }    // AES加密数据
    public byte[] Signature { get; set; }                 // 数据签名
    public byte[] RsaEncryptedAesKey { get; set; }        // RSA加密的AES密钥
    
    public string ToJson()                                // 序列化为JSON
    public static HybridEncryptData FromJson(string json) // 从JSON反序列化
}
```

## 安全注意事项

1. **密钥管理**：请妥善保管加密密钥，建议使用安全的密钥管理系统
2. **RSA 数据长度限制**：RSA 加密有数据长度限制，对于 2048 位密钥，最大数据长度为 190 字节
3. **混合加密**：对于大数据量，建议使用混合加密方案
4. **签名验证**：混合加密会自动验证数据签名，确保数据完整性和身份认证
5. **异常处理**：加密解密操作可能抛出异常，请做好异常处理

## 异常类型

- `ArgumentNullException`：参数为 null 时抛出
- `ArgumentException`：参数格式不正确时抛出
- `FileNotFoundException`：文件不存在时抛出
- `CryptographicException`：加密解密失败或签名验证失败时抛出

## 示例代码

### 完整的加密解密示例
```csharp
using System;
using System.Text;
using OSharp.Security;

class Program
{
    static void Main()
    {
        // AES 加密示例
        string originalText = "这是一个测试数据";
        var (encryptData, aesKey) = Crypto.AesEncrypt(originalText, null);
        string decryptedText = Crypto.AesDecrypt(encryptData, Convert.ToBase64String(aesKey));
        Console.WriteLine($"AES 解密结果: {decryptedText}");
        
        // RSA 加密示例
        var (publicKey, privateKey) = Crypto.GenerateRsaKey();
        string rsaEncrypted = Crypto.RsaEncrypt(originalText, publicKey);
        string rsaDecrypted = Crypto.RsaDecrypt(rsaEncrypted, privateKey);
        Console.WriteLine($"RSA 解密结果: {rsaDecrypted}");
        
        // 混合加密示例
        var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
        var (facePublicKey, facePrivateKey) = Crypto.GenerateRsaKey();
        
        string hybridEncrypted = Crypto.HybridEncrypt(originalText, ownPrivateKey, facePublicKey);
        string hybridDecrypted = Crypto.HybridDecrypt(hybridEncrypted, facePrivateKey, ownPublicKey);
        Console.WriteLine($"混合加密解密结果: {hybridDecrypted}");
    }
}
```

## 版本信息

- **版本**：1.0.0
- **作者**：郭明锋
- **最后更新**：2025-10-18
- **许可证**：Copyright (c) 2025 66SOFT. All rights reserved.
