(() => {
    const controls = document.getElementById('care-view-controls');
    const views = document.querySelector('.care-body-views');
    const siteForm = document.getElementById('care-site-form');

    if (controls && views) {
        const figures = [...views.querySelectorAll('[data-care-view]')];
        const buttons = [...controls.querySelectorAll('[data-care-select-view]')];
        const selectView = value => {
            figures.forEach(figure => { figure.hidden = figure.dataset.careView !== value; });
            buttons.forEach(button => button.setAttribute('aria-pressed', String(button.dataset.careSelectView === value)));
        };
        const position = (marker, x, y) => {
            marker.style.left = x / 10 + '%';
            marker.style.top = y / 10 + '%';
            marker.hidden = false;
        };

        views.classList.add('is-interactive');
        controls.hidden = false;
        buttons.forEach(button => button.addEventListener('click', () => selectView(button.dataset.careSelectView)));
        selectView(figures[0].dataset.careView);
        views.querySelectorAll('[data-care-x][data-care-y]').forEach(marker =>
            position(marker, Number(marker.dataset.careX), Number(marker.dataset.careY)));

        if (siteForm) {
            const entryKind = siteForm.elements.kind;
            const pointLabel = document.getElementById('care-point-label');
            const clear = document.getElementById('care-clear-point');
            const hideSelections = () => views.querySelectorAll('.care-selection').forEach(marker => { marker.hidden = true; });
            const clearPoint = () => {
                siteForm.elements.view.value = '';
                siteForm.elements.positionX.value = '';
                siteForm.elements.positionY.value = '';
                hideSelections();
                pointLabel.textContent = '';
                clear.hidden = true;
            };
            clear.addEventListener('click', () => {
                clearPoint();
                entryKind.focus();
            });
            entryKind.addEventListener('change', () => {
                if (entryKind.value === 'Observation') {
                    clearPoint();
                    siteForm.elements.region.value = '';
                }
            });
            siteForm.elements.region.addEventListener('change', () => {
                if (siteForm.elements.region.value && entryKind.value === 'Observation') entryKind.value = 'Finding';
            });
            figures.forEach(figure => {
                const surface = figure.querySelector('.care-body-image');
                surface.addEventListener('click', event => {
                    if (event.target.closest('a')) return;
                    const rect = surface.getBoundingClientRect();
                    if (rect.width === 0 || rect.height === 0) return;
                    const coordinate = (value, start, length) => Math.round(1000 * Math.min(1, Math.max(0, (value - start) / length)));
                    const x = coordinate(event.clientX, rect.left, rect.width);
                    const y = coordinate(event.clientY, rect.top, rect.height);
                    siteForm.elements.view.value = figure.dataset.careView;
                    siteForm.elements.positionX.value = x;
                    siteForm.elements.positionY.value = y;
                    if (entryKind.value === 'Observation') entryKind.value = 'Finding';
                    hideSelections();
                    position(figure.querySelector('.care-selection'), x, y);
                    pointLabel.textContent = pointLabel.dataset.pointText + ' ' + figure.querySelector('figcaption').textContent;
                    clear.hidden = false;
                });
            });
        }
    }

    const canvas = document.getElementById('care-weight-chart');
    if (!canvas) return;
    const summary = document.getElementById('care-weight-scale');
    const points = [...document.querySelectorAll('[data-weight][data-time]')]
        .map(row => [Number(row.dataset.time), Number(row.dataset.weight)])
        .sort((a, b) => a[0] - b[0]);
    if (points.length < 2) return;
    const low = points.reduce((min, point) => Math.min(min, point[1]), points[0][1]);
    const high = points.reduce((max, point) => Math.max(max, point[1]), points[0][1]);
    const duration = points.at(-1)[0] - points[0][0];
    const format = new Intl.NumberFormat(document.documentElement.lang, { style: 'unit', unit: 'gram', unitDisplay: 'short' });
    const dates = new Intl.DateTimeFormat(document.documentElement.lang, { dateStyle: 'short', timeZone: canvas.dataset.zone || undefined });
    canvas.hidden = false;
    summary.hidden = false;
    summary.textContent = format.format(low) + ' – ' + format.format(high) + ' · ' +
        dates.formatRange(new Date(points[0][0]), new Date(points.at(-1)[0]));

    const draw = () => {
        const context = canvas.getContext('2d');
        const ratio = window.devicePixelRatio || 1;
        const { width, height } = canvas.getBoundingClientRect();
        canvas.width = width * ratio;
        canvas.height = height * ratio;
        context.scale(ratio, ratio);
        const inset = 16;
        const coordinates = points.map((point, index) => [
            inset + (duration ? (point[0] - points[0][0]) / duration : index / (points.length - 1)) * (width - 2 * inset),
            height - inset - (high === low ? 0.5 : (point[1] - low) / (high - low)) * (height - 2 * inset)
        ]);
        context.strokeStyle = '#dce6dd';
        for (let row = 0; row <= 4; row++) {
            const y = inset + row * (height - 2 * inset) / 4;
            context.beginPath();
            context.moveTo(inset, y);
            context.lineTo(width - inset, y);
            context.stroke();
        }
        context.strokeStyle = '#20583f';
        context.lineWidth = 3;
        context.beginPath();
        coordinates.forEach(([x, y], index) => index === 0 ? context.moveTo(x, y) : context.lineTo(x, y));
        context.stroke();
        context.fillStyle = '#20583f';
        coordinates.forEach(([x, y]) => {
            context.beginPath();
            context.arc(x, y, 3.5, 0, Math.PI * 2);
            context.fill();
        });
    };
    new ResizeObserver(draw).observe(canvas);
})();
