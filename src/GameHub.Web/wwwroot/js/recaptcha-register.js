// reCAPTCHA v3 no formulário de cadastro.
// Fluxo: no submit, seguramos o envio → pedimos um TOKEN ao Google (grecaptcha.execute)
// → colocamos o token no campo oculto → aí sim o form vai pro servidor, que valida o token.
//
// Detalhe do Blazor: com "enhanced navigation" (SSR), trocar de página NÃO reexecuta scripts.
// Por isso registramos o init também no evento 'enhancedload' do Blazor — assim o gancho é
// (re)aplicado sempre que uma navegação leva até a página de cadastro.
(function () {
    function init() {
        var campo = document.getElementById('recaptcha-token');
        if (!campo || !window.__recaptchaSiteKey || typeof grecaptcha === 'undefined') return;

        var form = campo.closest('form');
        if (!form || form.dataset.recaptchaHooked) return;   // não duplica o gancho
        form.dataset.recaptchaHooked = 'true';

        form.addEventListener('submit', function (e) {
            if (campo.value) return;              // já tem token → deixa o envio seguir
            e.preventDefault();                   // segura o envio
            e.stopPropagation();
            grecaptcha.ready(function () {
                grecaptcha.execute(window.__recaptchaSiteKey, { action: 'register' })
                    .then(function (token) {
                        campo.value = token;      // token no campo oculto
                        form.requestSubmit();     // reenviar (agora com token, o if lá em cima deixa passar)
                    });
            });
        }, true);
    }

    document.addEventListener('DOMContentLoaded', init);
    window.addEventListener('load', init);
    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', init);
    }
})();
