using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.Hubs;

[Authorize] // exige autenticacion jwt
public class FunkosHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = Context.User?.IsInRole("ADMIN") ?? false;

        if (userId is not null) {
            // grupo privado del user
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        if (isAdmin) {
            // grupo de admins
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        await base.OnConnectedAsync();
    }
}