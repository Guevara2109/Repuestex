(function () {
    var body = document.body;

    var overlay = document.createElement('div');
    overlay.className = 'rx-overlay';
    overlay.innerHTML = '<div class="rx-overlay-spin"></div>';
    body.appendChild(overlay);

    window.rxSubmit = function (form) {
        body.classList.add('rx-busy');
        form.submit();
    };

    window.rxToast = function (mensaje) {
        var cont = document.getElementById('rx-toasts');
        if (!cont || !window.bootstrap) return;
        var el = document.createElement('div');
        el.className = 'toast align-items-center text-bg-dark border-0';
        el.setAttribute('role', 'status');
        el.innerHTML = '<div class="d-flex"><div class="toast-body"></div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button></div>';
        el.querySelector('.toast-body').textContent = mensaje;
        cont.appendChild(el);
        var t = new bootstrap.Toast(el, { delay: 2400 });
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
        t.show();
    };

    document.addEventListener('submit', function (e) {
        var f = e.target;
        if (f && f.hasAttribute && f.hasAttribute('data-loading')) body.classList.add('rx-busy');
    });
    window.addEventListener('pageshow', function () { body.classList.remove('rx-busy'); });

    document.querySelectorAll('[data-autosubmit]').forEach(function (form) {
        form.addEventListener('change', function (e) {
            if (e.target.matches('input[type=radio], input[type=checkbox], select')) window.rxSubmit(form);
        });
    });

    document.addEventListener('click', function (e) {
        var b = e.target.closest('[data-toggle-password]');
        if (!b) return;
        var input = b.parentElement.querySelector('input');
        if (!input) return;
        var mostrar = input.type === 'password';
        input.type = mostrar ? 'text' : 'password';
        var icono = b.querySelector('i');
        if (icono) icono.className = mostrar ? 'bi bi-eye-slash' : 'bi bi-eye';
        b.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
    });

    function copiar(texto) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(texto);
        }
        return new Promise(function (ok, fallo) {
            var ta = document.createElement('textarea');
            ta.value = texto;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy') ? ok() : fallo(); } catch (err) { fallo(err); }
            ta.remove();
        });
    }

    document.addEventListener('click', function (e) {
        var b = e.target.closest('[data-copy]');
        if (!b) return;
        var texto = b.getAttribute('data-copy');
        copiar(texto).then(function () {
            window.rxToast('Copiado: ' + texto);
        }, function () {
            window.rxToast('No se pudo copiar');
        });
    });

    var nav = document.getElementById('rxNav');
    if (nav) {
        var revisar = function () { nav.classList.toggle('scrolled', window.scrollY > 4); };
        window.addEventListener('scroll', revisar, { passive: true });
        revisar();
    }
})();
