/* MedSafe platform — progressive enhancement only.
   Every action also works as a normal form post without JavaScript. */
(function () {
  'use strict';

  /* ---------- auto dismiss toasts ---------- */
  document.querySelectorAll('[data-auto-dismiss]').forEach(function (el) {
    setTimeout(function () {
      el.style.transition = 'opacity .3s, transform .3s';
      el.style.opacity = '0';
      el.style.transform = 'translateY(-6px)';
      setTimeout(function () { el.remove(); }, 320);
    }, 4200);
  });

  /* ---------- search suggestions ---------- */
  var input = document.getElementById('q');
  var box = document.getElementById('suggest');

  if (input && box) {
    var timer = null;
    var activeIndex = -1;

    function hide() { box.classList.add('hidden'); box.innerHTML = ''; activeIndex = -1; }

    function render(items) {
      if (!items.length) { hide(); return; }
      box.innerHTML = items.map(function (s) {
        return '<div class="sugitem" data-value="' + s.text.replace(/"/g, '&quot;') + '">' +
               '<span>' + s.text.replace(/</g, '&lt;') + '</span>' +
               '<span class="kind">' + s.kind + '</span></div>';
      }).join('');
      box.classList.remove('hidden');
      box.querySelectorAll('.sugitem').forEach(function (row) {
        row.addEventListener('mousedown', function (e) {
          e.preventDefault();
          input.value = row.getAttribute('data-value');
          hide();
          input.form.submit();
        });
      });
    }

    input.addEventListener('input', function () {
      var q = input.value.trim();
      clearTimeout(timer);
      if (!q) { hide(); return; }
      timer = setTimeout(function () {
        fetch('/Notices/Suggest?q=' + encodeURIComponent(q), { headers: { 'Accept': 'application/json' } })
          .then(function (r) { return r.ok ? r.json() : []; })
          .then(render)
          .catch(hide);
      }, 180);
    });

    input.addEventListener('keydown', function (e) {
      var rows = box.querySelectorAll('.sugitem');
      if (e.key === 'Escape') { hide(); return; }
      if (!rows.length) return;
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        activeIndex += (e.key === 'ArrowDown' ? 1 : -1);
        if (activeIndex < 0) activeIndex = rows.length - 1;
        if (activeIndex >= rows.length) activeIndex = 0;
        rows.forEach(function (r, i) { r.classList.toggle('active', i === activeIndex); });
      }
      if (e.key === 'Enter' && activeIndex >= 0) {
        e.preventDefault();
        input.value = rows[activeIndex].getAttribute('data-value');
        hide();
        input.form.submit();
      }
    });

    input.addEventListener('blur', function () { setTimeout(hide, 120); });
  }

  /* ---------- checkbox selection: post immediately ---------- */
  document.querySelectorAll('form[data-autosubmit] input[type=checkbox], form[data-autosubmit] select').forEach(function (el) {
    el.addEventListener('change', function () { el.form.submit(); });
  });

  /* ---------- inline comment editing ---------- */
  document.querySelectorAll('[data-edit-toggle]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var target = document.getElementById(btn.getAttribute('data-edit-toggle'));
      if (!target) return;
      target.classList.toggle('hidden');
    });
  });

  /* ---------- confirm before destructive posts ---------- */
  document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (e) {
      if (!window.confirm(form.getAttribute('data-confirm'))) e.preventDefault();
    });
  });

  /* ---------- collapsible panels ---------- */
  document.querySelectorAll('[data-toggle-panel]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var panel = document.getElementById(btn.getAttribute('data-toggle-panel'));
      if (!panel) return;
      panel.classList.toggle('hidden');
      var firstField = panel.querySelector('input,select,textarea');
      if (firstField && !panel.classList.contains('hidden')) firstField.focus();
    });
  });

  /* ---------- keep focus in the search box after a re-sort ---------- */
  var sortSelect = document.getElementById('sortSel');
  if (sortSelect) {
    sortSelect.addEventListener('change', function () {
      if (sortSelect.form) sortSelect.form.submit();
    });
  }
})();
