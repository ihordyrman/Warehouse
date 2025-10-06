using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Warehouse.Core.Infrastructure.Common;

public class EncryptedStringConverter(IDataProtector protector) : ValueConverter<string, string>(
    plainText => protector.Protect(plainText),
    cipherText => protector.Unprotect(cipherText));
