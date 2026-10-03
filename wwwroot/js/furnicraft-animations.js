/* FurniCraft - small animation helpers
   Save as: wwwroot/js/furnicraft-animations.js
   - Navbar shrinks slightly after scrolling
   - Cards / sections fade in when they scroll into view
   - Hero numbers count up
   Nothing here changes data or talks to the server. */
(function () {
    'use strict';

    var reduceMotion = window.matchMedia &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /* ---------- Navbar state ---------- */
    var nav = document.querySelector('.fc-navbar');
    if (nav) {
        var onScroll = function () {
            nav.classList.toggle('is-scrolled', window.scrollY > 10);
        };
        onScroll();
        window.addEventListener('scroll', onScroll, { passive: true });
    }

    if (reduceMotion || !('IntersectionObserver' in window)) {
        return;
    }

    /* ---------- Count-up numbers (e.g. +100, 100%) ---------- */
    function countUp(el) {
        var match = /^(\D*)(\d+)(\D*)$/.exec(el.textContent.trim());
        if (!match) return;

        var prefix = match[1], target = parseInt(match[2], 10), suffix = match[3];
        var duration = 1400, start = null;

        function step(ts) {
            if (start === null) start = ts;
            var p = Math.min((ts - start) / duration, 1);
            var eased = 1 - Math.pow(1 - p, 3);
            el.textContent = prefix + Math.round(target * eased) + suffix;
            if (p < 1) requestAnimationFrame(step);
        }

        el.textContent = prefix + '0' + suffix;
        requestAnimationFrame(step);
    }

    var statObserver = new IntersectionObserver(function (entries, obs) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                countUp(entry.target);
                obs.unobserve(entry.target);
            }
        });
    }, { threshold: 0.6 });

    document.querySelectorAll('.fc-hero-stats strong').forEach(function (el) {
        statObserver.observe(el);
    });

    /* ---------- Scroll reveal ---------- */
    var selector = [
        '.fc-section-header',
        '.fc-category-card',
        '.fc-product-card',
        '.fc-feature-card',
        '.fc-panel',
        '.fc-cta',
        '.fc-empty-state'
    ].join(',');

    var revealObserver = new IntersectionObserver(function (entries, obs) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;

            var el = entry.target;
            el.classList.add('is-visible');
            obs.unobserve(el);

            // Remove helper classes afterwards so normal hover effects
            // (which use their own transitions) are not slowed down.
            var delay = parseFloat(el.style.getPropertyValue('--fc-delay')) || 0;
            window.setTimeout(function () {
                el.classList.remove('fc-reveal', 'is-visible');
                el.style.removeProperty('--fc-delay');
            }, 800 + delay * 1000);
        });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

    document.querySelectorAll(selector).forEach(function (el) {
        // Already on screen at load: leave it alone (no flash)
        if (el.getBoundingClientRect().top < window.innerHeight * 0.9) return;

        // Stagger items that sit side by side in the same row
        var siblings = el.parentElement && el.parentElement.parentElement
            ? Array.prototype.slice.call(el.parentElement.parentElement.children)
            : [];
        var index = siblings.indexOf(el.parentElement);
        if (index > -1) {
            el.style.setProperty('--fc-delay', (Math.min(index, 5) * 0.08) + 's');
        }

        el.classList.add('fc-reveal');
        revealObserver.observe(el);
    });
})();
