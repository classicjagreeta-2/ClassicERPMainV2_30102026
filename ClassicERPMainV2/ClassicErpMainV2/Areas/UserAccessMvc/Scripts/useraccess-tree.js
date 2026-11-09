
(function (root) {
    'use strict';

    var STATE = { GRANTED: 'granted', DENIED: 'denied', UNAVAILABLE: 'unavailable' };
    var AEDV_LETTERS = ['A', 'E', 'D', 'V'];
    var AEDV_LABELS = { A: 'Add', E: 'Edit', D: 'Delete', V: 'View' };

    var editable = false;
    function setEditable(value) { editable = !!value; }
    function isEditable() { return editable; }

    function isLeafGroup(node) {
        return node.Type === 'E' || node.Type === 'E1';
    }

    function rightKey(mnuId, indexOra, AddLockDays, EditLockDays, DeleteLockDays) {

        return mnuId + '!' + (indexOra || ''); // + '!' + (AddLockDays || '') + '!' + (EditLockDays || '') + '!' + (DeleteLockDays || '');
    }

    function buildRightsIndex(rights) {
        var byKey = {};
        (rights || []).forEach(function (r) {
            byKey[rightKey(r.MnuId, r.IndexOra)] = r;
           // byKey[rightKey(r.MnuId, r.IndexOra, r.AddLockDays, r.EditLockDays, r.DeleteLockDays)] = r;
        });
        return byKey;
    }

    function rightStateOf(node, rightsByKey) {
        var right = rightsByKey[rightKey(node.Id, node.IndexOra)];
        if (!right) return STATE.DENIED;

        if (isLeafGroup(node)) {
            return (right.Aedv && right.Aedv.length > 0) ? STATE.GRANTED : STATE.DENIED;
        }

        if (right.Aedv && right.Aedv.length > 0) {
            return STATE.GRANTED;
        }

        // Parent / Section nodes exist in USRACS with empty AEDV
        return STATE.GRANTED;
    }

    function refreshLeafDots(leafNode) {
        if (!leafNode._aedvDots) return;
        AEDV_LETTERS.forEach(function (letter) {
            var el = leafNode._aedvDots[letter];
            if (!el) return;
            var granted = leafNode._aedv.indexOf(letter) !== -1;
            el.className = 'ua-dot ua-dot-' + (granted ? STATE.GRANTED : STATE.DENIED);
        });
    }

    function toggleAedvLetter(leafNode, letter) {
        var has = leafNode._aedv.indexOf(letter) !== -1;
        leafNode._aedv = has ? leafNode._aedv.split(letter).join('') : leafNode._aedv + letter;
        refreshLeafDots(leafNode);
        leafNode._state = leafNode._aedv.length > 0 ? STATE.GRANTED : STATE.DENIED;
        if (leafNode._dotEl) leafNode._dotEl.className = 'ua-dot ua-dot-' + leafNode._state;
        paintStateUp(leafNode);
    }


    function renderAedvRow(leafNode, letter, doc) {
        var li = doc.createElement('li');
        li.className = 'ua-tree-node ua-aedv-node';

        var row = doc.createElement('div');
        row.className = 'ua-node-row ua-aedv-row';

        // Caret
        var caret = doc.createElement('span');
        caret.className = 'ua-caret ua-leaf';
        row.appendChild(caret);

        // Dot
        var granted = leafNode._aedv.indexOf(letter) !== -1;

        var dot = doc.createElement('span');
        dot.className = 'ua-dot ua-dot-' +
            (granted ? STATE.GRANTED : STATE.DENIED);

        leafNode._aedvDots = leafNode._aedvDots || {};
        leafNode._aedvDots[letter] = dot;
        row.appendChild(dot);
        var label = doc.createElement('span');
        //label.className = 'ua-node-name';
        label.className = 'ua-node-name ua-aedv-' + letter.toLowerCase();
        var lockDays = {
            A: leafNode._addLockDays || 0,
            E: leafNode._editLockDays || 0,
            D: leafNode._deleteLockDays || 0,
            V: 0
        }[letter];

        label.setAttribute('data-days', lockDays);
        label.textContent = AEDV_LABELS[letter];
        // Tooltip on hover
        label.title = AEDV_LABELS[letter] + ' Lock Days: ' + lockDays;
        label._leafNode = leafNode;
        label._letter = letter;
        //label.setAttribute('data-lockDays', lockDays);
        row.appendChild(label);

        li.appendChild(row);
        function handleAedvClick(e) {
            e.preventDefault();
            e.stopPropagation();

            if (!editable) return;
            if (leafNode._state === STATE.UNAVAILABLE) return;

            toggleAedvLetter(leafNode, letter);
        }

        dot.addEventListener('click', handleAedvClick);
        /*  label.addEventListener('click', function () { alert(); });*/

        return li;
    }
    function renderNode(node, rightsByKey, doc) {
        var li = doc.createElement('li');
        li.className = 'ua-tree-node';

        var row = doc.createElement('div');
        row.className = 'ua-node-row';

        var leaf = isLeafGroup(node);
        var hasRealChildren = node.Children && node.Children.length > 0;
        var expandable = hasRealChildren || leaf; // leaf nodes expand into the 4 synthetic AEDV rows

        var caret = doc.createElement('span');
        caret.className = 'ua-caret' + (expandable ? '' : ' ua-leaf');
        caret.textContent = expandable ? '▸' : '';
        row.appendChild(caret);

        var state = rightStateOf(node, rightsByKey);
        var dot = doc.createElement('span');
        dot.className = 'ua-dot ua-dot-' + state;
        node._dotEl = dot;
        node._state = state;

        if (leaf) {
            var right = rightsByKey[rightKey(node.Id, node.IndexOra)];
            node._aedv = (right && right.Aedv) ? right.Aedv : '';
            node._addLockDays =
                right && right.AddLockDays != null
                    ? Number(right.AddLockDays)
                    : 0;

            node._editLockDays =
                right && right.EditLockDays != null
                    ? Number(right.EditLockDays)
                    : 0;

            node._deleteLockDays =
                right && right.DeleteLockDays != null
                    ? Number(right.DeleteLockDays)
                    : 0;
            //if (right && Object.prototype.hasOwnProperty.call(right, 'AddLockDays')) {
            //    node._addLockDays = right.AddLockDays || 0;
            //}

            //if (right && Object.prototype.hasOwnProperty.call(right, 'EditLockDays')) {
            //    node._editLockDays = right.EditLockDays || 0;
            //}

            //if (right && Object.prototype.hasOwnProperty.call(right, 'DeleteLockDays')) {
            //    node._deleteLockDays = right.DeleteLockDays || 0;
            //}
        }
        row.appendChild(dot);

        var label = doc.createElement('span');
        label.className = 'ua-node-name';
        label.textContent = node.Name;
        row.appendChild(label);

        li.appendChild(row);
        node._rowEl = row;
        node._liEl = li;

        var childList = null;
        if (expandable) {
            childList = doc.createElement('ul');
            childList.style.display = 'none'; // collapsed by default, matches "Collapse all" resting state

            if (leaf) {
                AEDV_LETTERS.forEach(function (letter) {
                    childList.appendChild(renderAedvRow(node, letter, doc));
                });
            } else {
                node.Children.forEach(function (child) {
                    childList.appendChild(renderNode(child, rightsByKey, doc));
                });
            }

            li.appendChild(childList);

            caret.addEventListener('click', function (e) {
                e.stopPropagation();
                var collapsed = childList.style.display === 'none';
                childList.style.display = collapsed ? 'block' : 'none';
                caret.textContent = collapsed ? '▾' : '▸';
            });
        }

        row.addEventListener('click', function (e) {
            if (e.target === caret) return;
            if (!editable) return;
            if (node._state === STATE.UNAVAILABLE) return;
            var next = node._state === STATE.GRANTED ? STATE.DENIED : STATE.GRANTED;
            paintStateDown(node, next);
            paintStateUp(node);
        });

        return li;
    }

    function paintStateDown(node, state) {
        if (node._state === STATE.UNAVAILABLE) return;
        node._state = state;
        if (node._dotEl) node._dotEl.className = 'ua-dot ua-dot-' + state;

        if (isLeafGroup(node)) {
            node._aedv = state === STATE.GRANTED ? 'AEDV' : '';
            refreshLeafDots(node);
        }

        (node.Children || []).forEach(function (c) { paintStateDown(c, state); });
    }

    function paintStateUp(node) {
        var parent = node._parent;
        if (!parent) return;

        // Parent turns GRANTED if at least one direct child is GRANTED
        var anyChildGranted = (parent.Children || []).some(function (child) {
            return child._state === STATE.GRANTED;
        });

        var nextState = anyChildGranted ? STATE.GRANTED : STATE.DENIED;
        if (parent._state !== STATE.UNAVAILABLE) {
            parent._state = nextState;
            if (parent._dotEl) parent._dotEl.className = 'ua-dot ua-dot-' + nextState;
        }
        paintStateUp(parent);
    }

    function updateAllParentStatesOnLoad(treeData) {
        function resolveNodeState(node) {
            if (node.Children && node.Children.length > 0 && !isLeafGroup(node)) {
                node.Children.forEach(resolveNodeState);
                var anyChildGranted = node.Children.some(function (c) {
                    return c._state === STATE.GRANTED;
                });
                if (node._state !== STATE.UNAVAILABLE) {
                    node._state = anyChildGranted ? STATE.GRANTED : STATE.DENIED;
                    if (node._dotEl) {
                        node._dotEl.className = 'ua-dot ua-dot-' + node._state;
                    }
                }
            } else if (isLeafGroup(node)) {
                node._state = (node._aedv && node._aedv.length > 0) ? STATE.GRANTED : STATE.DENIED;
                if (node._dotEl && node._state !== STATE.UNAVAILABLE) {
                    node._dotEl.className = 'ua-dot ua-dot-' + node._state;
                }
            }
        }
        treeData.forEach(resolveNodeState);
    }

    function linkParents(nodes, parent) {
        nodes.forEach(function (n) {
            n._parent = parent;
            if (n.Children && n.Children.length)
                linkParents(n.Children, n);
        });
    }

    function renderTree(container, treeData, rights) {
        const result = rights.find(user => user.AddLockDays > 0);
        linkParents(treeData, null);
        var rightsByKey = buildRightsIndex(rights);
        container.innerHTML = '';
        var ul = document.createElement('ul');
        ul.className = 'ua-tree-root';
        treeData.forEach(function (node) {
            ul.appendChild(renderNode(node, rightsByKey, document));
        });
        container.appendChild(ul);
        updateAllParentStatesOnLoad(treeData);
        return treeData;
    }

    function walk(nodes, fn) {
        nodes.forEach(function (n) {
            fn(n);
            if (n.Children && n.Children.length) walk(n.Children, fn);
        });
    }

    function expandAll(treeData) {
        walk(treeData, function (n) {
            if (n._liEl) {
                var childList = n._liEl.querySelector(':scope > ul');
                if (childList) childList.style.display = 'block';
            }
            var caret = n._rowEl && n._rowEl.querySelector('.ua-caret');
            if (caret && !caret.classList.contains('ua-leaf')) caret.textContent = '▾';
        });
    }

    function collapseAll(treeData) {
        walk(treeData, function (n) {
            if (n._liEl) {
                var childList = n._liEl.querySelector(':scope > ul');
                if (childList) childList.style.display = 'none';
            }
            var caret = n._rowEl && n._rowEl.querySelector('.ua-caret');
            if (caret && !caret.classList.contains('ua-leaf')) caret.textContent = '▸';
        });
    }

    function filterTree(treeData, query) {
        query = (query || '').trim().toLowerCase();

        function apply(node) {
            var childMatches = (node.Children || []).map(apply);
            var selfMatch = query === '' || (node.Name || '').toLowerCase().indexOf(query) !== -1;
            var anyChildMatch = childMatches.some(function (m) { return m; });
            var visible = selfMatch || anyChildMatch;

            if (node._liEl) node._liEl.classList.toggle('ua-node-hidden', !visible);

            if (query !== '' && anyChildMatch && node._liEl) {
                var childList = node._liEl.querySelector(':scope > ul');
                if (childList) childList.style.display = 'block';
                var caret = node._rowEl && node._rowEl.querySelector('.ua-caret');
                if (caret && !caret.classList.contains('ua-leaf')) caret.textContent = '▾';
            }

            return visible;
        }

        treeData.forEach(apply);
    }

    function selectAllColumn(treeData, column, checked) {
        if (!editable) return; // same gate as row clicks — select-all only acts in Edit/Add mode

        walk(treeData, function (node) {
            if (node._liEl && node._liEl.classList.contains('ua-node-hidden')) return;
            if (node._state === STATE.UNAVAILABLE) return;

            if (column === 'all') {
                paintStateDown(node, checked ? STATE.GRANTED : STATE.DENIED);
            } else if (isLeafGroup(node)) {
                var letter = { add: 'A', edit: 'E', delete: 'D', view: 'V' }[column];
                var has = node._aedv.indexOf(letter) !== -1;
                if (checked && !has) node._aedv += letter;
                if (!checked && has) node._aedv = node._aedv.split(letter).join('');
                refreshLeafDots(node);
                node._state = node._aedv.length > 0 ? STATE.GRANTED : STATE.DENIED;
                if (node._dotEl) node._dotEl.className = 'ua-dot ua-dot-' + node._state;
            }
        });

        updateAllParentStatesOnLoad(treeData);
    }

    function serializeRights(rootWrapper) {
        var rights = [];
        var seen = {};

        function visit(node) {
            var key = (node.Id || '') + '!' + (node.IndexOra || '');

            if (isLeafGroup(node)) {
                if (node._aedv && node._aedv.length > 0) {
                    if (!seen[key]) {
                        seen[key] = true;
                        rights.push({
                            mnuId: node.Id,
                            indexOra: node.IndexOra,
                            aedv: node._aedv,
                            addLockDays: Number(node._addLockDays || 0),
                            editLockDays: Number(node._editLockDays || 0),
                            deleteLockDays: Number(node._deleteLockDays || 0)
                        });
                    }
                }
            } else if (node._state === STATE.GRANTED && node.Children && node.Children.length > 0) {
                if (!seen[key]) {
                    seen[key] = true;
                    rights.push({
                        mnuId: node.Id,
                        indexOra: node.IndexOra,
                        aedv: '',
                        addLockDays: 0,
                        editLockDays: 0,
                        deleteLockDays: 0
                    });
                }
            } else if (node._state === STATE.GRANTED && (!node.Children || node.Children.length === 0)) {
                if (!seen[key]) {
                    seen[key] = true;
                    rights.push({
                        mnuId: node.Id,
                        indexOra: node.IndexOra,
                        aedv: node._aedv || 'X',
                        addLockDays: 0,
                        editLockDays: 0,
                        deleteLockDays: 0
                    });
                }
            }

            (node.Children || []).forEach(visit);
        }

        (rootWrapper.Children || []).forEach(visit);
        return rights;
    }

    root.UserAccessTree = {
        serializeRights: serializeRights,
        renderTree: renderTree,
        expandAll: expandAll,
        collapseAll: collapseAll,
        filterTree: filterTree,
        selectAllColumn: selectAllColumn,
        setEditable: setEditable,
        isEditable: isEditable
    };
})(window);
