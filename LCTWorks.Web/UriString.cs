using LCTWorks.Web.Extensions;

namespace LCTWorks.Web;

/// <summary>
/// Represents a URI string wrapper that provides validation, normalization, and comparison capabilities.
/// </summary>
public partial class UriString : IEquatable<UriString>, IEquatable<string>
{
    private const string GatekeepPattern = "^https?:\\/\\/(?:www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b(?:[-a-zA-Z0-9()@:%_\\+.~#?&\\/=]*)$";
    private static readonly Uri EmptyUri = new("about:blank");
    private bool? _isValid;
    private string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="UriString"/> class.
    /// </summary>
    /// <param name="value">The URI string value.</param>
    /// <param name="validate">If <see langword="true"/>, validates and normalizes the URI to HTTPS scheme immediately.</param>
    public UriString(string value, bool validate = true)
    {
        if (value == null)
        {
            _isValid = false;
            _value = value ?? string.Empty;
        }
        else
        {
            _value = value.ToLowerInvariant();
            if (validate)
            {
                Validate();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the URI can be parsed as a valid absolute HTTP or HTTPS URI.
    /// </summary>
    public bool IsValid => _isValid ??= TryCreateUri(out _);

    /// <summary>
    /// Gets the underlying URI string value.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets a value indicating whether the URI matches the standard URL pattern using regex validation.
    /// </summary>
    public bool IsStrictlyValidUrl() => _isValid ??= ValidUrlRegex().IsMatch(_value);

    /// <summary>
    /// Asynchronously determines whether the URI represents valid image data.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/>
    /// if the URI is valid and contains valid image data; otherwise, <see langword="false"/>.</returns>
    public Task<bool> IsValidImageDataAsync()
    {
        if (!IsValid)
        {
            return Task.FromResult(false);
        }
        return Value.IsValidImageDataUrlAsync();
    }

    /// <summary>
    /// Attempts to create an absolute HTTP or HTTPS URI from the current value.
    /// </summary>
    /// <remarks>This method only succeeds if the current value represents a well-formed absolute URI with an
    /// HTTP or HTTPS scheme. The output parameter is set to <see langword="null"/> if the operation fails.</remarks>
    /// <param name="uri">When this method returns, contains the created <see cref="Uri"/> if the operation succeeds; otherwise,
    /// <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a valid absolute HTTP or HTTPS URI is created; otherwise, <see langword="false"/>.</returns>
    public bool TryCreateUri(out Uri uri)
    {
        if (_isValid == false || string.IsNullOrWhiteSpace(_value))
        {
            uri = EmptyUri;
            return false;
        }
        _isValid = TryCreateUriInternal(out uri);
        return _isValid.Value;
    }

    /// <summary>
    /// Validates the URI string and normalizes it to HTTPS scheme.
    /// </summary>
    /// <returns><see langword="true"/> if the URI is valid and was normalized successfully; otherwise, <see langword="false"/>.</returns>
    public bool Validate()
    {
        if (_isValid.HasValue)
        {
            return _isValid.Value;
        }
        if (string.IsNullOrWhiteSpace(_value))
        {
            _isValid = false;
            return false;
        }
        _isValid = ValidateInternal();
        return _isValid.Value;
    }

    private bool TryCreateUriInternal(out Uri uri)
    {
        try
        {
            if (Uri.TryCreate(_value, UriKind.Absolute, out var validatedUrl))
            {
                if (validatedUrl.Scheme == Uri.UriSchemeHttp || validatedUrl.Scheme == Uri.UriSchemeHttps)
                {
                    uri = validatedUrl;
                    return true;
                }
            }
            var urlWithScheme = $"{Uri.UriSchemeHttps}://{_value}";
            if (Uri.TryCreate(urlWithScheme, UriKind.Absolute, out validatedUrl))
            {
                if (validatedUrl.Host.Contains('.') &&
                    (validatedUrl.Scheme == Uri.UriSchemeHttp || validatedUrl.Scheme == Uri.UriSchemeHttps))
                {
                    uri = validatedUrl;
                    return true;
                }
            }
        }
        catch
        {
        }
        uri = EmptyUri;
        return false;
    }

    private bool ValidateInternal()
    {
        try
        {
            var builder = new UriBuilder(_value)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1,
            };
            _value = builder.Uri.AbsoluteUri;
            return true;
        }
        catch (Exception)
        {
            _isValid = false;
            return false;
        }
    }

    #region Operators and Comparisons

    public static explicit operator string(UriString uriString)
    {
        return uriString._value;
    }

    public static explicit operator Uri?(UriString uriString)
    {
        uriString.TryCreateUri(out var uri);
        return uri;
    }

    public static implicit operator UriString(string value)
    {
        return new UriString(value);
    }

    public static implicit operator UriString(Uri uri)
    {
        return new UriString(uri.AbsoluteUri, validate: false);
    }

    public static bool operator !=(UriString? left, string? right)
    {
        return !(left == right);
    }

    public static bool operator !=(string? left, UriString? right)
    {
        return !(left == right);
    }

    public static bool operator !=(UriString? left, UriString? right)
    {
        return !(left == right);
    }

    public static bool operator ==(UriString? left, string? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left._value == right;
    }

    public static bool operator ==(string? left, UriString? right)
    {
        if (right is null)
        {
            return left is null;
        }
        return left == right._value;
    }

    public static bool operator ==(UriString? left, UriString? right)
    {
        if (left is null)
        {
            return right is null;
        }
        if (right is null)
        {
            return false;
        }
        return left._value == right._value;
    }

    public bool Equals(UriString? other)
    {
        if (other is null)
        {
            return false;
        }
        return _value == other._value;
    }

    public bool Equals(string? other)
    {
        return _value == other;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            UriString uriString => Equals(uriString),
            string str => Equals(str),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return _value?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        return _value;
    }

    [System.Text.RegularExpressions.GeneratedRegex(GatekeepPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase, "es-ES")]
    private static partial System.Text.RegularExpressions.Regex ValidUrlRegex();

    #endregion Operators and Comparisons
}