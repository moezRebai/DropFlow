using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Users;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services.Email.Templates;

public class EmailTemplateService(ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    public string GetWelcomeTemplate(string firstName, string url)
    {
        logger.LogDebug("Generating welcome email template for {FirstName}", firstName);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 50px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 16px;
            opacity: 0.95;
        }}
        .content {{
            padding: 40px 35px;
            color: #333;
        }}
        .content h2 {{
            color: #667eea;
            font-size: 24px;
            margin-top: 0;
        }}
        .content p {{
            line-height: 1.7;
            margin: 15px 0;
            font-size: 15px;
        }}
        .feature-box {{
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            border-radius: 8px;
            padding: 25px;
            margin: 25px 0;
        }}
        .feature-list {{
            list-style: none;
            padding: 0;
            margin: 15px 0 0 0;
        }}
        .feature-list li {{
            padding: 10px 0;
            display: flex;
            align-items: center;
            font-size: 15px;
        }}
        .feature-list li::before {{
            content: '✓';
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            width: 24px;
            height: 24px;
            border-radius: 50%;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-right: 12px;
            font-weight: bold;
            flex-shrink: 0;
        }}
        .cta-button {{
            display: inline-block;
            padding: 16px 40px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            margin: 25px 0;
            box-shadow: 0 4px 10px rgba(102, 126, 234, 0.4);
            transition: transform 0.2s;
        }}
        .cta-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 14px rgba(102, 126, 234, 0.5);
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px;
            text-align: center;
            color: #666;
            font-size: 13px;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            margin: 5px 0;
        }}
        .divider {{
            height: 1px;
            background: linear-gradient(to right, transparent, #ddd, transparent);
            margin: 30px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚚 DropFlow</h1>
            <p>Gestion de Livraison Intelligente</p>
        </div>
        
        <div class='content'>
            <h2>Bienvenue {firstName} ! 🎉</h2>
            
            <p>Nous sommes ravis de vous accueillir sur <strong>DropFlow</strong>, votre nouvelle plateforme de gestion de livraisons.</p>
            
            <p>Votre compte a été créé avec succès et vous pouvez dès maintenant profiter de toutes nos fonctionnalités.</p>
            
            <div class='feature-box'>
                <h3 style='margin-top: 0; color: #333;'>🎯 Ce que vous pouvez faire :</h3>
                <ul class='feature-list'>
                    <li>Gérer vos livraisons en temps réel</li>
                    <li>Optimiser vos tournées avec l'IA</li>
                    <li>Suivre vos clients et factures</li>
                    <li>Générer des rapports détaillés</li>
                    <li>Gérer votre stock efficacement</li>
                    <li>Collaborer avec votre équipe</li>
                </ul>
            </div>
            
            <center>
                <a href='{url}' class='cta-button'>
                    Commencer maintenant →
                </a>
            </center>
            
            <div class='divider'></div>
            
            <p style='color: #666; font-size: 14px;'>
                💡 <strong>Besoin d'aide ?</strong><br>
                Notre équipe support est disponible pour vous accompagner dans vos premiers pas.
            </p>
        </div>
        
        <div class='footer'>
            <p><strong>© 2025 DropFlow</strong> - Tous droits réservés</p>
            <p>Cet email a été envoyé automatiquement, merci de ne pas y répondre.</p>
        </div>
    </div>
</body>
</html>";
    }
    public string GetUserInvitationTemplate(string companyName, string inviteUrl)
    {
        logger.LogDebug("Generating invitation email template for company {CompanyName}", companyName);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 50px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 16px;
            opacity: 0.95;
        }}
        .content {{
            padding: 40px 35px;
            color: #333;
        }}
        .content h2 {{
            color: #667eea;
            font-size: 24px;
            margin-top: 0;
        }}
        .content p {{
            line-height: 1.7;
            margin: 15px 0;
            font-size: 15px;
        }}
        .company-badge {{
            display: inline-block;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            color: white;
            padding: 8px 20px;
            border-radius: 25px;
            font-weight: 600;
            font-size: 16px;
            margin: 10px 0;
        }}
        .info-box {{
            background-color: #f8f9ff;
            border-left: 4px solid #667eea;
            padding: 20px;
            margin: 25px 0;
            border-radius: 4px;
        }}
        .info-box p {{
            margin: 8px 0;
            font-size: 14px;
        }}
        .cta-button {{
            display: inline-block;
            padding: 18px 45px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 17px;
            margin: 25px 0;
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
            transition: all 0.3s ease;
        }}
        .cta-button:hover {{
            transform: translateY(-3px);
            box-shadow: 0 6px 16px rgba(102, 126, 234, 0.5);
        }}
        .warning-box {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning-box p {{
            margin: 0;
            color: #856404;
            font-size: 13px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px;
            text-align: center;
            color: #666;
            font-size: 13px;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚚 DropFlow</h1>
            <p>Invitation à rejoindre l'équipe</p>
        </div>
        
        <div class='content'>
            <h2>Vous avez été invité ! 🎊</h2>
            
            <p>Vous avez été invité à rejoindre :</p>
            <center>
                <div class='company-badge'>{companyName}</div>
            </center>
            
            <p>sur la plateforme <strong>DropFlow</strong>, la solution complète de gestion de livraisons.</p>
            
            <div class='info-box'>
                <p><strong>📦 Avec DropFlow, vous pourrez :</strong></p>
                <p>• Gérer les livraisons en temps réel</p>
                <p>• Optimiser vos tournées automatiquement</p>
                <p>• Suivre les clients et la facturation</p>
                <p>• Collaborer efficacement avec votre équipe</p>
            </div>
            
            <p>Cliquez sur le bouton ci-dessous pour créer votre compte et définir votre mot de passe :</p>
            
            <center>
                <a href='{inviteUrl}' class='cta-button'>
                    Accepter l'invitation →
                </a>
            </center>
            
            <div class='warning-box'>
                <p>⚠️ <strong>Important :</strong> Ce lien d'invitation expire dans <strong>72 heures</strong>. Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email en toute sécurité.</p>
            </div>
            
            <p style='color: #999; font-size: 13px; margin-top: 30px;'>
                Si le bouton ne fonctionne pas, copiez-collez ce lien dans votre navigateur :<br>
                <a href='{inviteUrl}' style='color: #667eea; word-break: break-all;'>{inviteUrl}</a>
            </p>
        </div>
        
        <div class='footer'>
            <p><strong>© 2025 DropFlow</strong> - Tous droits réservés</p>
            <p>Cet email a été envoyé automatiquement, merci de ne pas y répondre.</p>
        </div>
    </div>
</body>
</html>";
    }
    public string GetPasswordResetTemplate(string userName, string resetUrl)
    {
        logger.LogDebug("Generating password reset email template for {UserName}", userName);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            color: white;
            padding: 50px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 700;
        }}
        .header-icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .content {{
            padding: 40px 35px;
            color: #333;
        }}
        .content h2 {{
            color: #f5576c;
            font-size: 24px;
            margin-top: 0;
        }}
        .content p {{
            line-height: 1.7;
            margin: 15px 0;
            font-size: 15px;
        }}
        .cta-button {{
            display: inline-block;
            padding: 18px 45px;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 17px;
            margin: 25px 0;
            box-shadow: 0 4px 12px rgba(245, 87, 108, 0.4);
            transition: all 0.3s ease;
        }}
        .cta-button:hover {{
            transform: translateY(-3px);
            box-shadow: 0 6px 16px rgba(245, 87, 108, 0.5);
        }}
        .alert-box {{
            background-color: #fff3cd;
            border: 2px solid #ffc107;
            padding: 20px;
            margin: 25px 0;
            border-radius: 8px;
        }}
        .alert-box h3 {{
            margin: 0 0 12px 0;
            color: #856404;
            font-size: 16px;
        }}
        .alert-box ul {{
            margin: 10px 0 0 0;
            padding-left: 25px;
        }}
        .alert-box li {{
            color: #856404;
            margin: 8px 0;
            font-size: 14px;
        }}
        .security-note {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin: 25px 0;
        }}
        .security-note p {{
            margin: 8px 0;
            font-size: 14px;
            color: #555;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px;
            text-align: center;
            color: #666;
            font-size: 13px;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='header-icon'>🔐</div>
            <h1>Réinitialisation du mot de passe</h1>
        </div>
        
        <div class='content'>
            <h2>Demande de réinitialisation</h2>
            
            <p>Bonjour <strong>{userName}</strong>,</p>
            
            <p>Vous avez demandé à réinitialiser votre mot de passe pour votre compte <strong>DropFlow</strong>.</p>
            
            <p>Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe sécurisé :</p>
            
            <center>
                <a href='{resetUrl}' class='cta-button'>
                    Réinitialiser mon mot de passe →
                </a>
            </center>
            
            <div class='alert-box'>
                <h3>⚠️ Informations importantes :</h3>
                <ul>
                    <li>Ce lien expire dans <strong>24 heures</strong></li>
                    <li>Le lien ne peut être utilisé qu'<strong>une seule fois</strong></li>
                    <li>Votre mot de passe actuel reste valide jusqu'à ce que vous en définissiez un nouveau</li>
                    <li>Si vous n'avez pas demandé cette réinitialisation, <strong>ignorez cet email</strong></li>
                </ul>
            </div>
            
            <div class='security-note'>
                <p><strong>🛡️ Conseils de sécurité :</strong></p>
                <p>• Utilisez un mot de passe unique et complexe</p>
                <p>• Minimum 8 caractères avec majuscules, chiffres et symboles</p>
                <p>• Ne partagez jamais votre mot de passe</p>
                <p>• Changez régulièrement votre mot de passe</p>
            </div>
            
            <p style='color: #999; font-size: 13px; margin-top: 30px;'>
                Si le bouton ne fonctionne pas, copiez-collez ce lien dans votre navigateur :<br>
                <a href='{resetUrl}' style='color: #f5576c; word-break: break-all;'>{resetUrl}</a>
            </p>
        </div>
        
        <div class='footer'>
            <p><strong>© 2025 DropFlow</strong> - Tous droits réservés</p>
            <p>Pour toute question concernant la sécurité de votre compte, contactez notre support.</p>
        </div>
    </div>
</body>
</html>";
    }

    public string GetDeliveryNoteTemplate(string deliveryReference, string clientName, string address)
    {
        // À implémenter plus tard si besoin
        return string.Empty;
    }

    public string GetInvoiceTemplate(string invoiceNumber, decimal amount)
    {
        // À implémenter plus tard si besoin
        return string.Empty;
    }
}