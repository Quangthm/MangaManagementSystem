# Setup secrets for MangaManagementSystem.Web
Write-Host "Setting up .NET User Secrets for MangaManagementSystem.Web..." -ForegroundColor Cyan

$projectPath = "src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj"

# Define secrets to set
$secrets = @{
    "Smtp:Username" = "thanhlong060206@gmail.com"
    "Smtp:Password" = "zrvx wltq wglp kjyu"
    "Smtp:FromEmail" = "thanhlong060206@gmail.com"
    "Authentication:Google:ClientId" = "399671906693-s9ef5nlmclqroddoektr7v4345ub6ch6.apps.googleusercontent.com"
    "Authentication:Google:ClientSecret" = "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"
    "Cloudinary:CloudName" = "dvpbtdju8"
    "Cloudinary:ApiKey" = "911425412829516"
    "Cloudinary:ApiSecret" = "UtZqXPPZF2_2Ol7jk3DYI0A5B_k"
    "Recaptcha:SiteKey" = "6LcDEAstAAAAAIdZ40z465BLGSUKFOCDkeLO_KNz"
    "Recaptcha:SecretKey" = "6LcDEAstAAAAAF6rVrBJt9M6rt_7VuDjhj5i4Kc-"
}

foreach ($key in $secrets.Keys) {
    $value = $secrets[$key]
    Write-Host "Setting secret: $key"
    dotnet user-secrets set "$key" "$value" --project $projectPath
}

Write-Host "User Secrets successfully configured!" -ForegroundColor Green
