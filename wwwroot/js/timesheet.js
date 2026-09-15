// Spec 005 FR-017, SC-019: a day column is the unit of commit, so edits to a column you never
// pressed Save on are lost the moment you leave. Warn before that happens — and only warn, because
// nothing here is allowed to stop someone getting out of the page.
//
// This is a courtesy on top of a server that is already correct: every rule the grid appears to
// enforce is enforced again in TimesheetService. Turning JavaScript off loses the warning, not the
// rules.
(function () {
    'use strict';

    var grid = document.getElementById('grid');
    if (!grid) {
        return;
    }

    // day (yyyy-MM-dd) -> day name, for days whose inputs have been touched since the last save.
    var dirty = new Map();
    var leaving = false;

    function cellOf(element) {
        return element.closest ? element.closest('.ts-cell') : null;
    }

    grid.addEventListener('input', function (event) {
        var cell = cellOf(event.target);
        if (cell && cell.dataset.day) {
            dirty.set(cell.dataset.day, cell.dataset.dayName);
        }
    });

    function dirtyDaysExcept(day) {
        var names = [];
        dirty.forEach(function (name, key) {
            if (key !== day) {
                names.push(name);
            }
        });
        return names;
    }

    function sentence(names) {
        if (names.length === 1) {
            return names[0] + ' has unsaved hours.';
        }

        return names.slice(0, -1).join(', ') + ' and ' + names[names.length - 1] +
            ' have unsaved hours.';
    }

    // Returns false when the user chose to go back and save.
    function confirmLeaving(names) {
        if (names.length === 0) {
            return true;
        }

        var left = window.confirm(
            sentence(names) + ' Leaving now discards them.\n\n' +
            'OK to continue and lose them, or Cancel to go back and save.');

        if (left) {
            // They know. Do not ask a second time on the way out.
            leaving = true;
        }

        return left;
    }

    grid.addEventListener('submit', function (event) {
        var submitter = event.submitter;

        // Saving a column clears that column, and only that one: the other days are still unsaved
        // and the page is about to redirect away from them.
        var committing = submitter && submitter.name === 'day' ? submitter.value : null;

        if (!confirmLeaving(dirtyDaysExcept(committing))) {
            event.preventDefault();
            return;
        }

        leaving = true;
    });

    // Adding or removing a row also posts and redirects, which discards every typed cell.
    var addRow = document.getElementById('add-row');
    if (addRow) {
        addRow.addEventListener('submit', function (event) {
            if (!confirmLeaving(dirtyDaysExcept(null))) {
                event.preventDefault();
            }
        });
    }

    // Week navigation and any other link off the page (FR-017 names changing week explicitly).
    document.addEventListener('click', function (event) {
        var link = event.target.closest('a[href]');
        if (!link || link.target === '_blank' || link.getAttribute('href').startsWith('#')) {
            return;
        }

        if (!confirmLeaving(dirtyDaysExcept(null))) {
            event.preventDefault();
        }
    });

    // The catch-all for back, forward, and closing the tab. The browser shows its own wording here;
    // all a handler can do is ask for the prompt.
    window.addEventListener('beforeunload', function (event) {
        if (leaving || dirty.size === 0) {
            return;
        }

        event.preventDefault();
        event.returnValue = '';
    });
})();
