using System.Security.Cryptography;
using System.Text;

namespace CodingAgent.Services.GitHub.Infrastructure;

/// <summary>
/// Validates GitHub webhook signatures using HMAC-SHA256.
/// </summary>
public class WebhookValidator
{
    private readonly string _secret;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookValidator"/> class.
    /// </summary>
    /// <param name="secret">The webhook secret configured in GitHub.</param>
    public WebhookValidator(string secret)
    {
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
    }

    /// <summary>
    /// Validates the GitHub webhook signature.
    /// </summary>
    /// <param name="payload">The raw request payload.</param>
    /// <param name="signature">The signature from the X-Hub-Signature-256 header.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public bool ValidateSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = "sha256=" + Convert.ToHexString(computedHash).ToLower();

        return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
    }
}
