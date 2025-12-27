namespace LCTWorks.Web;

public class UriString : IEquatable<UriString>, IEquatable<string>
{
    private static readonly Uri EmptyUri = new("about:blank");
    private bool? _isValid;
    private string _value;

    public UriString(string value, bool validate = true)
    {
        if (value == null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }

        _value = value.ToLowerInvariant();
        if (validate)
        {
            Validate();
        }
    }

    public bool IsValid => _isValid ??= TryCreateUri(out _);

    public string Value => _value;

    /// <summary>
    /// Attempts to create an absolute HTTP or HTTPS URI from the current value.
    /// </summary>
    /// <remarks>This method only succeeds if the current value represents a well-formed absolute URI with an
    /// HTTP or HTTPS scheme. The output parameter is set to <see langword="null"/> if the operation fails.</remarks>
    /// <param name="uri">When this method returns, contains the created <see cref="Uri"/> if the operation succeeds; otherwise, <see
    /// langword="null"/>.</param>
    /// <returns>true if a valid absolute HTTP or HTTPS URI is created; otherwise, false.</returns>
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
    /// <returns></returns>
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

    #endregion Operators and Comparisons
}