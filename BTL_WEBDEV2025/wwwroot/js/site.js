document.addEventListener('DOMContentLoaded', function() {
    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Spin on wheel for Shop by Detail
    const sbd = document.querySelector('.shop-by-detail');
    if (sbd) {
        const track = sbd.querySelector('.sbd-track');
        if (track) {
            // Calculate width of one base set (first 7 items)
            const items = Array.from(track.querySelectorAll('.sbd-item'));
            const baseSet = items.slice(0, 7);
            const baseWidth = baseSet.reduce((w, el) => w + el.getBoundingClientRect().width + parseFloat(getComputedStyle(track).gap || 0), 0);

            // Initialize in the middle set for seamless circular scroll
            let initialized = false;
            const initPosition = () => {
                if (initialized || !baseWidth) return;
                track.scrollLeft = baseWidth; // jump to start of duplicated set
                initialized = true;
            };
            // Wait next frame to ensure layout computed
            requestAnimationFrame(initPosition);

            // Auto-scroll loop with gentle constant velocity
            let targetLeft = 0;
            let rafId = 0;
            let velocity = 0.28; // px per frame
            const lerp = (a, b, t) => a + (b - a) * t;

            const animate = () => {
                const current = track.scrollLeft;
                const next = lerp(current, targetLeft, 0.08);
                track.scrollLeft = next;

                // Wrap edges for seamless loop
                if (track.scrollLeft <= 0) {
                    track.scrollLeft += baseWidth;
                    targetLeft += baseWidth;
                } else if (track.scrollLeft >= baseWidth * 2) {
                    track.scrollLeft -= baseWidth;
                    targetLeft -= baseWidth;
                }

                // Move target continuously
                targetLeft += velocity;

                rafId = requestAnimationFrame(animate);
            };

            const startAnimation = () => {
                if (!rafId) rafId = requestAnimationFrame(animate);
            };

            // Remove wheel interaction: auto only

            // Pause on hover/focus, resume on leave
            track.addEventListener('mouseenter', () => { velocity = 0; });
            track.addEventListener('mouseleave', () => { velocity = 0.28; startAnimation(); });

            // Respect prefers-reduced-motion & only animate when visible
            const prefersReduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            if (prefersReduced) velocity = 0;
            const io = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && !prefersReduced) {
                        velocity = 0.28; startAnimation();
                    } else { velocity = 0; }
                });
            }, { threshold: 0.1 });
            io.observe(track);

            document.addEventListener('visibilitychange', () => {
                if (document.hidden) velocity = 0; else if (!prefersReduced) { velocity = 0.28; startAnimation(); }
            });

            startAnimation();

            // Start when user interacts; pause when idle automatically
            track.addEventListener('mouseenter', () => { /* no auto scroll, only user-driven */ });
            track.addEventListener('mouseleave', () => { /* remain idle until next interaction */ });
        }
    }
});

