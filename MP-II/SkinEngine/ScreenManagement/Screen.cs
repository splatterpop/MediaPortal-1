#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screen;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine
{
  /// <summary>
  /// screen class respresenting a logical screen represented by a particular skin.
  /// </summary>
  public class Screen: NameScope
  {
    #region Enums

    public enum State
    {
      Opening,
      Running,
      Closing
    }

    #endregion

    #region Variables

    private string _name;
    private bool _hasFocus;
    private State _state = State.Running;

    /// <summary>
    /// Our handler bound on our KeyPressed handler. Will be used to attach to
    /// the <see cref="IInputManager"/>'s KeyPressed event.
    /// </summary>
    private KeyPressedHandler _keyPressHandler;

    /// <summary>
    /// Our handler bound on our MouseMoved handler. Will be used to attach to
    /// the <see cref="IInputManager"/>'s MouseMoved event.
    /// </summary>
    private MouseMoveHandler _mouseMoveHandler;

    /// <summary>
    /// Holds the information if our input handlers are currently attached at
    /// the <see cref="IInputManger"/>.
    /// </summary>
    private bool _attachedInput = false;

    private Property _opened;
    private Thread _thread;
    public event EventHandler OnClose;
    private bool _history;
    UIElement _visual;
    bool _setFocusedElement = false;
    Animator _animator;
    List<IUpdateEventHandler> _invalidControls = new List<IUpdateEventHandler>();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Screen"/> class.
    /// </summary>
    /// <param name="name">The logical screen name.</param>
    public Screen(string name)
    {
      if (name == null)
      {
        throw new ArgumentNullException("name");
      }
      if (name.Length == 0)
      {
        throw new ArgumentOutOfRangeException("name");
      }

      _history = true;
      _opened = new Property(typeof(bool), true);
      _name = name;
      _keyPressHandler = OnKeyPressed;
      _mouseMoveHandler = OnMouseMove;
      _animator = new Animator();
    }

    public Animator Animator
    {
      get { return _animator; }
    }

    public UIElement Visual
    {
      get { return _visual; }
      set
      {
        _visual = value;
        if (_visual != null)
        {
          _history = _visual.History;
          _visual.IsArrangeValid = true;
          _visual.SetWindow(this);
        }
      }
    }

    public FrameworkElement RootElement
    {
      get { return _visual as FrameworkElement; }
    }

    public bool History
    {
      get { return _history; }
      set { _history = value; }
    }

    /// <summary>
    /// Returns if this window is still open or if it should be closed
    /// </summary>
    /// <value><c>true</c> if this window is still open; otherwise, <c>false</c>.</value>
    public bool IsOpened
    {
      get { return (bool)_opened.GetValue(); }
      set { _opened.SetValue(value); }
    }

    public Property IsOpenedProperty
    {
      get { return _opened; }
      set { _opened = value; }
    }

    public State ScreenState
    {
      get { return _state; }
      set { _state = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this window has focus.
    /// </summary>
    /// <value><c>true</c> if this window has focus; otherwise, <c>false</c>.</value>
    public bool HasFocus
    {
      get { return _hasFocus; }
      set { _hasFocus = value; }
    }

    public bool IsAnimating
    {
      get { return false; }
    }

    /// <summary>
    /// Renders this window.
    /// </summary>
    public void Render()
    {
      uint time = (uint)Environment.TickCount;
      SkinContext.TimePassed = time;
      SkinContext.FinalMatrix = new ExtendedMatrix();

      if (SkinContext.UseBatching)
      {
        lock (_visual)
        {
          _animator.Animate();
          Update();
        }
        return;
      }
      else
      {

        lock (_visual)
        {
          _visual.Render();
          _animator.Animate();
        }
      }
      if (_setFocusedElement)
      {
        if (_visual.FocusedElement != null)
        {
          _visual.FocusedElement.HasFocus = true;
          _setFocusedElement = !_visual.FocusedElement.HasFocus;
        }
      }
    }

    public void AttachInput()
    {
      if (!_attachedInput)
      {
        ServiceScope.Get<IInputManager>().OnKeyPressed += _keyPressHandler;
        ServiceScope.Get<IInputManager>().OnMouseMove += _mouseMoveHandler;
        _attachedInput = true;
        HasFocus = true;
      }
    }

    /// <summary>
    /// Called when window should be shown
    /// </summary>
    public void Show()
    {
      Trace.WriteLine("screen Show: " + Name);
      FocusManager.FocusedElement = null;
      SkinContext.IsValid = false;
      lock (_visual)
      {
        _invalidControls.Clear();
        if (SkinContext.UseBatching)
          _visual.DestroyRenderTree();
        _visual.Deallocate();
        _visual.Allocate();
        _visual.Invalidate();
        _visual.Initialize();
        if (SkinContext.UseBatching)
          _visual.BuildRenderTree();
        _setFocusedElement = true;
        SkinContext.IsValid = true;
      }
    }

    /// <summary>
    /// Called when window should be hidden
    /// </summary>
    public void Hide()
    {
      Trace.WriteLine("screen Hide: " + Name);
      lock (_visual)
      {
        Animator.StopAll();
        if (SkinContext.UseBatching)
          _visual.DestroyRenderTree();
        _visual.Deallocate();
        _invalidControls.Clear();
      }
    }

    /// <summary>
    /// Called when a keypress has been received
    /// </summary>
    /// <param name="key">The key.</param>
    private void OnKeyPressed(ref Key key)
    {
      if (!HasFocus || !_attachedInput)
      {
        return;
      }
      _visual.OnKeyPressed(ref key);
    }

    /// <summary>
    /// Called when the mouse was moved
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    private void OnMouseMove(float x, float y)
    {
      if (!HasFocus || !_attachedInput)
      {
        return;
      }
      _visual.OnMouseMove(x, y);
    }

    /// <summary>
    /// called when the window should close
    /// </summary>
    public void DetachInput()
    {
      if (_attachedInput)
      {
        ServiceScope.Get<IInputManager>().OnKeyPressed -= _keyPressHandler;
        ServiceScope.Get<IInputManager>().OnMouseMove -= _mouseMoveHandler;
        _attachedInput = false;
        if (OnClose != null)
        {
          OnClose(this, null);
        }
      }
    }

    public void Invalidate(IUpdateEventHandler ctl)
    {
      if (SkinContext.UseBatching == false) 
        return;
      if (!SkinContext.IsValid)
      {
        return;
      }
      FrameworkElement el = (FrameworkElement)ctl;
      if (el.IsArrangeValid == false)
      {
        return;
      }
      lock (_invalidControls)
      {
        if (!_invalidControls.Contains(ctl))
          _invalidControls.Add(ctl);
      }

    }

    void Update()
    {
      List<IUpdateEventHandler> ctls;
      lock (_invalidControls)
      {
        if (_invalidControls.Count == 0) 
          return;
        ctls = _invalidControls;
        _invalidControls = new List<IUpdateEventHandler>();
      }
      for (int i = 0; i < ctls.Count; ++i)
      {
        ctls[i].Update();
      }
    }

    #region IWindow implementation
    /// <summary>
    /// Gets the window-name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name; }
    }

    public void Reset()
    {
      Trace.WriteLine("screen Reset: " + Name);
      GraphicsDevice.InitializeZoom();
      _visual.Invalidate();
      _visual.Initialize();
    }
    #endregion
  }
}
