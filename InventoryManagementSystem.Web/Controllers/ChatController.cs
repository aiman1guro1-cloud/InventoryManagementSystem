using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IChatRepository _chatRepository;

        public ChatController(UserManager<ApplicationUser> userManager, IChatRepository chatRepository)
        {
            _userManager = userManager;
            _chatRepository = chatRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var allUsers = _userManager.Users
                .Where(u => u.Id != currentUser.Id && u.IsActive)
                .OrderBy(u => u.UserName)
                .ToList();

            var usersWithStatus = new List<object>();

            foreach (var user in allUsers)
            {
                var unreadCount = await _chatRepository.GetUnreadCountAsync(currentUser.Id);
                var isOnline = await _chatRepository.IsUserOnlineAsync(user.Id);

                usersWithStatus.Add(new
                {
                    user.Id,
                    Name = user.UserName,
                    IsOnline = isOnline,
                    UnreadCount = unreadCount
                });
            }

            return Json(usersWithStatus);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var messages = await _chatRepository.GetConversationAsync(currentUser.Id, userId);

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string senderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await _chatRepository.MarkMessagesAsReadAsync(currentUser.Id, senderId);

            return Ok();
        }
    }
}