using Microsoft.EntityFrameworkCore;
using OneClick.Server.Models;

namespace OneClick.Server.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. 카테고리
        var snsCat = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "SNS");
        if (snsCat == null)
        {
            snsCat = new Category { CategoryName = "SNS", Description = "소셜 미디어 자동화 도구", IconClass = "fa-solid fa-comment-dots" };
            context.Categories.Add(snsCat);
            await context.SaveChangesAsync();
        }

        // 2. 모듈 (KakaoSender)
        // 기존 모듈이 있는지 확인
        var existingModule = await context.Modules.FirstOrDefaultAsync(m => m.ModuleKey == "KakaoSender");
        
        // 최신 UI 스키마 정의
        string kakaoSchema = """
        {
            "title": "카카오톡 자동 발송",
            "description": "특정 채팅방을 찾아 메시지를 자동으로 발송합니다.",
            "fields": [
                {
                    "key": "roomName",
                    "label": "채팅방 이름 목록",
                    "type": "textarea",
                    "required": true,
                    "placeholder": "보낼 채팅방 이름들을 입력해주세요.\n예: 홍길동, 김철수",
                    "rows": 3
                },
                {
                    "key": "message",
                    "label": "발송할 메시지",
                    "type": "textarea",
                    "required": true,
                    "placeholder": "보내실 내용을 입력해주세요.\n여러 줄 입력 가능합니다.",
                    "rows": 5
                }
            ]
        }
        """;

        if (existingModule == null)
        {
            // 신규 생성
            context.Modules.Add(new Module
            {
                ModuleName = "카카오톡 자동 발송기", 
                Description = "친구 목록을 기반으로 개인화된 메시지를 자동으로 발송합니다.",
                CategoryId = snsCat.Id,
                ExecutionType = "ServerAutomation",
                ModuleKey = "KakaoSender",
                IconClass = "fa-solid fa-comment",
                IsActive = true,
                UiSchema = kakaoSchema
            });
            await context.SaveChangesAsync();
        }
        else
        {
            // 기존 모듈 업데이트 (스키마 동기화)
            existingModule.UiSchema = kakaoSchema;
            // 필요하다면 다른 필드도 업데이트
            existingModule.ModuleName = "카카오톡 자동 발송기";
            await context.SaveChangesAsync();
        }

        // 3. 테스트 쿠폰
        if (!await context.Coupons.AnyAsync(c => c.Code == "TEST2025"))
        {
            context.Coupons.Add(new Coupon
            {
                Code = "TEST2025",
                Type = CouponType.AllPass, // 올인원 패스
                DurationDays = 30,         // 30일
                ExpiresAt = DateTime.UtcNow.AddYears(1) // 1년 뒤 만료
            });
            await context.SaveChangesAsync();
        }
    }
}
