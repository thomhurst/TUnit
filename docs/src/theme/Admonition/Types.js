import React from 'react';
import DefaultAdmonitionTypes from '@theme-original/Admonition/Types';

function PerformanceAdmonition(props) {
  return (
    <DefaultAdmonitionTypes.tip {...props} title={props.title || 'Performance'}>
      {props.children}
    </DefaultAdmonitionTypes.tip>
  );
}

function FromXunitAdmonition(props) {
  return (
    <DefaultAdmonitionTypes.info {...props} title={props.title || 'Coming from xUnit'}>
      {props.children}
    </DefaultAdmonitionTypes.info>
  );
}

function FromNunitAdmonition(props) {
  return (
    <DefaultAdmonitionTypes.info {...props} title={props.title || 'Coming from NUnit'}>
      {props.children}
    </DefaultAdmonitionTypes.info>
  );
}

function FromMstestAdmonition(props) {
  return (
    <DefaultAdmonitionTypes.info {...props} title={props.title || 'Coming from MSTest'}>
      {props.children}
    </DefaultAdmonitionTypes.info>
  );
}

const AdmonitionTypes = {
  ...DefaultAdmonitionTypes,
  'performance': PerformanceAdmonition,
  'from-xunit': FromXunitAdmonition,
  'from-nunit': FromNunitAdmonition,
  'from-mstest': FromMstestAdmonition,
};

export default AdmonitionTypes;
