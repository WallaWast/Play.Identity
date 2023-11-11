using System;

namespace Play.Identity.Service.Exceptions;

[Serializable]
internal class UnknowUserException : Exception
{
    public UnknowUserException(Guid userId) : base($"Unknown user '{userId}'")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}