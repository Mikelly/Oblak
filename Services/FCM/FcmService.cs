using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Models.Api;
using Oblak.Services.MNE;
using System;

namespace Oblak.Services.FCM
{
    public class FcmService
    {
        private IConfiguration _configuration;
        private readonly ApplicationDbContext _db;
        readonly ILogger<FcmService> _logger;

        public FcmService(IConfiguration configuration, ApplicationDbContext db, ILogger<FcmService> logger)
        {
            _configuration = configuration;
            _db = db;
            _logger = logger;
        }
    
        public async Task RegisterFcmToken(UserDevice userDevice)
        {
            try
            {
                var existingRecord = await _db.UserDevices
                    .Where(ud => ud.UserId == userDevice.UserId && ud.DeviceId == userDevice.DeviceId)
                    .FirstOrDefaultAsync();

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.FcmToken = userDevice.FcmToken;
                    _db.Entry(existingRecord).State = EntityState.Modified;
                }
                else
                {
                    // Create new record
                    _db.UserDevices.Add(userDevice);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }            
        }

        public async Task SendFcmMessage(FcmMessage fcmMessage)
        {
            try
            {
                var messaging = FirebaseMessaging.DefaultInstance;
                var message = new Message()
                {
                    Notification = new Notification()
                    {
                        Body = fcmMessage.Body,
                        Title = fcmMessage.Title
                    },
                    Token = fcmMessage.DeviceToken
                };
                var result = await messaging.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
